using System.Text.Json;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Feed;
using MediatR;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace Blizka.App.UseCases.Onboarding;

/// <summary>
/// Завершает онбординг (T-2.3): проверяет, что шаги 1-3 заполнены, согласие дано и загружено хотя бы
/// одно фото, переводит пользователя в Active, переносит данные черновика в профиль (включая заведение
/// персистентного <c>UserFilter</c> из ShowGender/AgeRange/DatingGoals шага 2, T-5.4), начисляет
/// регистрационный бонус и бонусы за пороги ProfileCompleteness через <see cref="ISparksService.AwardAsync"/> (T-8.1).
/// </summary>
public sealed class CompleteOnboardingCommandHandler(
    IUserRepository userRepository,
    IOnboardingDraftRepository draftRepository,
    IUserConsentRepository consentRepository,
    IUserDatePreferenceRepository datePreferenceRepository,
    ISparksService sparksService,
    IUserFilterRepository userFilterRepository,
    IOptions<SparksOptions> sparksOptions)
    : IRequestHandler<CompleteOnboardingCommand, CompleteOnboardingResult>
{
    public async Task<CompleteOnboardingResult> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithProfileDataAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        // После B8 (spec 002) пользователь на момент complete должен быть в Onboarding (перевод происходит
        // при первом PATCH /api/onboarding/draft) — New тоже считается "ещё не готов", не отдельным случаем.
        if (user.Status != UserStatus.Onboarding)
        {
            throw new OnboardingAlreadyCompletedException(user.Id);
        }

        var draft = await draftRepository.GetAsync(request.UserId, cancellationToken);
        var stepData = ParseDraftData(draft?.DataJson);

        EnsureStepsComplete(stepData, user.Photos.Count);

        if (!await consentRepository.HasConsentAsync(request.UserId, ConsentType.TermsAndPrivacyPolicy, cancellationToken))
        {
            throw new OnboardingIncompleteException("consent");
        }

        ApplyProfileData(user, stepData);
        user.Status = UserStatus.Active;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        // "Дефолты при регистрации: из онбординга (шаг 2)" (T-5.4) — ShowGender/AgeRange/DatingGoals шага 2
        // сохраняются в новый UserFilter здесь же, одной транзакцией с остальными изменениями. Только для
        // новых пользователей: уже онбордившиеся до этой задачи бэкафилл не получают (см. заметку T-5.4) и
        // продолжают получать MVP-дефолты в GetFeedQueryHandler, пока сами не сохранят фильтры через PATCH.
        // Если строка уже есть — это повторный проход через DELETE /api/onboarding/draft (debug-сброс, см.
        // DeleteOnboardingDraftCommandHandler), который сознательно не трогает UserFilter: не пересоздаём
        // её здесь, иначе AddAsync конфликтует по PK_UserFilters с уже существующей строкой.
        if (await userFilterRepository.GetAsync(user.Id, cancellationToken) is null)
        {
            await userFilterRepository.AddAsync(BuildInitialUserFilter(user.Id, stepData), cancellationToken);
        }

        // RegistrationBonusAwardedAt — та же защита от повторного начисления, что и у порогов ProfileCompleteness
        // ниже: без неё DELETE /api/onboarding/draft (сброс Status обратно в New) + повторный проход до Complete
        // начислял бы RegistrationBonus заново на каждый круг.
        var sparksAwarded = 0;
        if (user.RegistrationBonusAwardedAt is null)
        {
            var registrationBonus = sparksOptions.Value.RegistrationBonusAmount;
            user.RegistrationBonusAwardedAt = DateTimeOffset.UtcNow;
            await sparksService.AwardAsync(user, registrationBonus, SparkTransactionType.RegistrationBonus, referenceId: null, cancellationToken);
            sparksAwarded = registrationBonus;
        }

        var datePreferenceCount = await datePreferenceRepository.CountByUserIdAsync(request.UserId, cancellationToken);
        user.ProfileCompleteness = ProfileCompletenessCalculator.Calculate(user, datePreferenceCount);
        sparksAwarded += await ProfileCompletenessBonusAwarder.AwardAsync(
            user, sparksService, sparksOptions.Value.ProfileCompletionThresholdBonusAmount, cancellationToken);

        try
        {
            await userRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUserUpdateException)
        {
            // Параллельный POST /complete для того же пользователя успел записать переход в Active первым —
            // отдаём тот же результат, что и при обычном повторном вызове, вместо задвоенного начисления зорок.
            throw new OnboardingAlreadyCompletedException(user.Id);
        }

        return new CompleteOnboardingResult(
            sparksAwarded,
            user.ProfileCompleteness,
            ProfileCompletenessCalculator.NextReward(user.ProfileCompleteness, request.Locale, sparksOptions.Value.ProfileCompletionThresholdBonusAmount),
            user.Status);
    }

    private static CombinedOnboardingData ParseDraftData(string? dataJson)
    {
        try
        {
            return JsonSerializer.Deserialize<CombinedOnboardingData>(dataJson ?? "{}", OnboardingDraftJson.Options)
                ?? new CombinedOnboardingData(null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new CombinedOnboardingData(null, null, null, null, null, null, null);
        }
    }

    private static void EnsureStepsComplete(CombinedOnboardingData data, int photoCount)
    {
        if (string.IsNullOrWhiteSpace(data.Name) || data.BirthDate is null || data.Gender is null)
        {
            throw new OnboardingIncompleteException("step1");
        }

        if (data.ShowGender is null || data.AgeRange is null || data.DatingGoals is null || data.DatingGoals.Count == 0)
        {
            throw new OnboardingIncompleteException("step2");
        }

        if (data.CityId is null)
        {
            throw new OnboardingIncompleteException("step3");
        }

        if (photoCount < 1)
        {
            throw new OnboardingIncompleteException("step4");
        }
    }

    private static void ApplyProfileData(User user, CombinedOnboardingData data)
    {
        user.Name = data.Name!;
        user.BirthDate = data.BirthDate!.Value;
        user.Gender = data.Gender!.Value;
        user.CityId = data.CityId!.Value;

        // User хранит одну основную цель знакомства, а шаг 2 позволяет выбрать несколько — берём первую
        // из выбранных как основную. Полный список целей уходит в UserFilter.DatingGoals (см. BuildInitialUserFilter).
        user.DatingGoal = data.DatingGoals!.First();

        // Геолокация — по желанию пользователя (spec 002, B1): при отказе Coordinates остаётся null,
        // и скоринг ленты падает на City.Coordinates (см. FeedCompatibilityScorer/GetFeedQueryHandler).
        if (data.Coordinates is { } coordinates)
        {
            user.Coordinates = new Point(coordinates.Lng, coordinates.Lat) { SRID = 4326 };
        }
    }

    private static UserFilter BuildInitialUserFilter(Guid userId, CombinedOnboardingData data) => new()
    {
        UserId = userId,
        ShowGender = data.ShowGender!.Value,
        AgeMin = data.AgeRange!.Min,
        AgeMax = data.AgeRange!.Max,
        MaxDistanceKm = UserFilterDefaults.MaxDistanceKm,
        DatingGoals = [.. data.DatingGoals!],
        RequirePhoto = UserFilterDefaults.RequirePhoto,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

}
