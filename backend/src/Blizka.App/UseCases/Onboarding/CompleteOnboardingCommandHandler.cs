using System.Text.Json;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Onboarding;

/// <summary>
/// Завершает онбординг (T-2.3): проверяет, что шаги 1-3 заполнены, согласие дано и загружено хотя бы
/// одно фото, переводит пользователя в Active, переносит данные черновика в профиль, начисляет
/// регистрационный бонус и бонусы за пороги ProfileCompleteness.
/// </summary>
public sealed class CompleteOnboardingCommandHandler(
    IUserRepository userRepository,
    IOnboardingDraftRepository draftRepository,
    IUserConsentRepository consentRepository,
    IUserDatePreferenceRepository datePreferenceRepository,
    ISparkTransactionRepository sparkTransactionRepository)
    : IRequestHandler<CompleteOnboardingCommand, CompleteOnboardingResult>
{
    private const int RegistrationBonusSparks = 50;

    public async Task<CompleteOnboardingResult> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithProfileDataAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        if (user.Status != UserStatus.New)
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

        var sparksAwarded = RegistrationBonusSparks;
        await AwardAsync(user, RegistrationBonusSparks, SparkTransactionType.RegistrationBonus, cancellationToken);

        var datePreferenceCount = await datePreferenceRepository.CountByUserIdAsync(request.UserId, cancellationToken);
        user.ProfileCompleteness = ProfileCompletenessCalculator.Calculate(user, datePreferenceCount);
        sparksAwarded += await AwardCompletenessBonusesAsync(user, cancellationToken);

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
            ProfileCompletenessCalculator.NextReward(user.ProfileCompleteness));
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
        // из выбранных как основную (ShowGender/AgeRange шага 2 сохранить пока негде: UserFilter появится в T-5.4).
        user.DatingGoal = data.DatingGoals!.First();
    }

    private async Task<int> AwardCompletenessBonusesAsync(User user, CancellationToken cancellationToken)
    {
        var totalAwarded = 0;
        var now = DateTimeOffset.UtcNow;

        if (user.ProfileCompleteness >= 60 && user.CompletenessBonus60AwardedAt is null)
        {
            user.CompletenessBonus60AwardedAt = now;
            await AwardAsync(user, ProfileCompletenessCalculator.ThresholdBonusSparks, SparkTransactionType.ProfileCompletion, cancellationToken);
            totalAwarded += ProfileCompletenessCalculator.ThresholdBonusSparks;
        }

        if (user.ProfileCompleteness >= 80 && user.CompletenessBonus80AwardedAt is null)
        {
            user.CompletenessBonus80AwardedAt = now;
            await AwardAsync(user, ProfileCompletenessCalculator.ThresholdBonusSparks, SparkTransactionType.ProfileCompletion, cancellationToken);
            totalAwarded += ProfileCompletenessCalculator.ThresholdBonusSparks;
        }

        if (user.ProfileCompleteness >= 100 && user.CompletenessBonus100AwardedAt is null)
        {
            user.CompletenessBonus100AwardedAt = now;
            await AwardAsync(user, ProfileCompletenessCalculator.ThresholdBonusSparks, SparkTransactionType.ProfileCompletion, cancellationToken);
            totalAwarded += ProfileCompletenessCalculator.ThresholdBonusSparks;
        }

        return totalAwarded;
    }

    private async Task AwardAsync(User user, int amount, SparkTransactionType type, CancellationToken cancellationToken)
    {
        user.SparksBalance += amount;

        await sparkTransactionRepository.AddAsync(
            new SparkTransaction
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Amount = amount,
                Type = type,
                BalanceAfter = user.SparksBalance,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            cancellationToken);
    }
}
