using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Cities;
using Blizka.App.UseCases.Feed;
using Blizka.App.UseCases.Onboarding;
using Blizka.App.UseCases.Users;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Interests;

/// <summary>
/// Обрабатывает <see cref="PatchUserInterestsCommand"/> (T-9.2): заменяет полный набор интересов
/// пользователя, при необходимости создаёт новые общие кастомные интересы (переиспользуя уже
/// существующий по точному названию, а не плодя дубликаты), пересчитывает ProfileCompleteness и
/// начисляет бонус за впервые достигнутый порог — тем же <see cref="ProfileCompletenessBonusAwarder"/>,
/// что и <see cref="PatchUserProfileCommandHandler"/> (T-9.1).
/// </summary>
public sealed class PatchUserInterestsCommandHandler(
    IUserRepository userRepository,
    IInterestRepository interestRepository,
    IUserDatePreferenceRepository datePreferenceRepository,
    ISparksService sparksService,
    IValidator<PatchUserInterestsCommand> validator,
    IOptions<SparksOptions> sparksOptions)
    : IRequestHandler<PatchUserInterestsCommand, PatchUserInterestsResult>
{
    public async Task<PatchUserInterestsResult> Handle(PatchUserInterestsCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await userRepository.GetByIdWithProfileDataAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        var catalogIds = request.InterestIds.Distinct().ToList();
        var catalogInterests = await interestRepository.GetByIdsAsync(catalogIds, cancellationToken);
        if (catalogInterests.Count != catalogIds.Count)
        {
            var missingId = catalogIds.First(id => catalogInterests.All(i => i.Id != id));
            throw new InterestNotFoundException(missingId);
        }

        var customInterests = new List<Interest>();
        foreach (var name in request.CustomInterests.Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var existing = await interestRepository.FindByNameAsync(name, cancellationToken)
                ?? catalogInterests.FirstOrDefault(i => string.Equals(i.NameRu, name, StringComparison.OrdinalIgnoreCase))
                ?? customInterests.FirstOrDefault(i => string.Equals(i.NameRu, name, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                customInterests.Add(existing);
                continue;
            }

            // Перевод на be/en недоступен — кастомный интерес хранится под одним и тем же названием
            // на всех локалях (решение по умолчанию, backend-spec.md в репозитории нет).
            var created = new Interest
            {
                Id = Guid.NewGuid(),
                Category = InterestCategory.Custom,
                NameRu = name,
                NameBe = name,
                NameEn = name,
                IsCustom = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            await interestRepository.AddAsync(created, cancellationToken);
            customInterests.Add(created);
        }

        var resolvedInterests = catalogInterests
            .Concat(customInterests)
            .DistinctBy(i => i.Id)
            .ToList();

        ApplyInterests(user, resolvedInterests);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var datePreferenceCount = await datePreferenceRepository.CountByUserIdAsync(request.UserId, cancellationToken);
        user.ProfileCompleteness = ProfileCompletenessCalculator.Calculate(user, datePreferenceCount);
        var sparksAwarded = await ProfileCompletenessBonusAwarder.AwardAsync(
            user, sparksService, sparksOptions.Value.ProfileCompletionThresholdBonusAmount, cancellationToken);

        try
        {
            await userRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUserUpdateException ex)
        {
            // Тот же принцип, что и в PatchUserProfileCommandHandler (T-9.1): переигрывание задвоило бы
            // начисление порогового бонуса и создание кастомных интересов, поэтому просто просим повторить.
            throw new ProfileUpdateConflictException(request.UserId, ex);
        }
        catch (ConcurrentInterestCreationException ex)
        {
            // Параллельный запрос успел создать интерес с тем же названием первым (уникальный индекс
            // IX_Interests_NameRu_Unique) — сам интерес уже есть в каталоге, просто просим повторить,
            // чтобы FindByNameAsync на этот раз его нашёл (T-9.2).
            throw new InterestCreationConflictException(ex.Name, ex);
        }

        var locale = CityLocaleResolver.Resolve(request.Locale);
        var nextReward = ProfileCompletenessCalculator.NextReward(
            user.ProfileCompleteness, request.Locale, sparksOptions.Value.ProfileCompletionThresholdBonusAmount);

        var interestsResult = resolvedInterests
            .Select(i => new InterestCatalogItemResult(i.Id, InterestNameResolver.Resolve(i, locale), i.IsCustom))
            .OrderBy(i => i.Name)
            .ToList();

        return new PatchUserInterestsResult(
            UserProfileMapper.ToResult(user, user.ProfileCompleteness, nextReward), sparksAwarded, interestsResult);
    }

    private static void ApplyInterests(User user, IReadOnlyCollection<Interest> interests)
    {
        var targetIds = interests.Select(i => i.Id).ToHashSet();

        foreach (var toRemove in user.UserInterests.Where(ui => !targetIds.Contains(ui.InterestId)).ToList())
        {
            user.UserInterests.Remove(toRemove);
        }

        var existingIds = user.UserInterests.Select(ui => ui.InterestId).ToHashSet();
        var now = DateTimeOffset.UtcNow;

        foreach (var interest in interests.Where(i => !existingIds.Contains(i.Id)))
        {
            user.UserInterests.Add(new UserInterest { UserId = user.Id, InterestId = interest.Id, CreatedAt = now });
        }
    }
}
