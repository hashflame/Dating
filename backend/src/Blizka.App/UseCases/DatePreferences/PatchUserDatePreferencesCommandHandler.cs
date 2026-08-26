using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Cities;
using Blizka.App.UseCases.Onboarding;
using Blizka.App.UseCases.Users;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.DatePreferences;

/// <summary>
/// Обрабатывает <see cref="PatchUserDatePreferencesCommand"/> (T-9.3): заменяет полный набор предпочтений
/// по формату свидания пользователя, пересчитывает ProfileCompleteness и начисляет бонус за впервые
/// достигнутый порог — тем же <see cref="ProfileCompletenessBonusAwarder"/>, что и
/// <see cref="Interests.PatchUserInterestsCommandHandler"/> (T-9.2).
/// </summary>
public sealed class PatchUserDatePreferencesCommandHandler(
    IUserRepository userRepository,
    IUserDatePreferenceRepository datePreferenceRepository,
    ISparksService sparksService,
    IValidator<PatchUserDatePreferencesCommand> validator,
    IOptions<SparksOptions> sparksOptions)
    : IRequestHandler<PatchUserDatePreferencesCommand, PatchUserDatePreferencesResult>
{
    public async Task<PatchUserDatePreferencesResult> Handle(
        PatchUserDatePreferencesCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await userRepository.GetByIdWithProfileDataAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        var catalog = await datePreferenceRepository.GetCatalogAsync(cancellationToken);
        var requestedCodes = request.Codes.Distinct().ToHashSet();
        var resolvedPreferences = catalog.Where(p => requestedCodes.Contains(p.Code)).ToList();

        ApplyPreferences(user, resolvedPreferences);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        user.ProfileCompleteness = ProfileCompletenessCalculator.Calculate(user, user.UserDatePreferences.Count);
        var sparksAwarded = await ProfileCompletenessBonusAwarder.AwardAsync(
            user, sparksService, sparksOptions.Value.ProfileCompletionThresholdBonusAmount, cancellationToken);

        try
        {
            await userRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUserUpdateException ex)
        {
            // Тот же принцип, что и в PatchUserInterestsCommandHandler (T-9.2): переигрывание задвоило бы
            // начисление порогового бонуса, поэтому просто просим повторить.
            throw new ProfileUpdateConflictException(request.UserId, ex);
        }

        var locale = CityLocaleResolver.Resolve(request.Locale);
        var nextReward = ProfileCompletenessCalculator.NextReward(
            user.ProfileCompleteness, request.Locale, sparksOptions.Value.ProfileCompletionThresholdBonusAmount);

        var preferencesResult = resolvedPreferences
            .Select(p => new DatePreferenceCatalogItemResult(p.Id, p.Code, DatePreferenceNameResolver.Resolve(p, locale)))
            .OrderBy(p => (int)p.Code)
            .ToList();

        return new PatchUserDatePreferencesResult(
            UserProfileMapper.ToResult(user, user.ProfileCompleteness, nextReward), sparksAwarded, preferencesResult);
    }

    private static void ApplyPreferences(User user, IReadOnlyCollection<DatePreference> preferences)
    {
        var targetIds = preferences.Select(p => p.Id).ToHashSet();

        foreach (var toRemove in user.UserDatePreferences.Where(p => !targetIds.Contains(p.DatePreferenceId)).ToList())
        {
            user.UserDatePreferences.Remove(toRemove);
        }

        var existingIds = user.UserDatePreferences.Select(p => p.DatePreferenceId).ToHashSet();
        var now = DateTimeOffset.UtcNow;

        foreach (var preference in preferences.Where(p => !existingIds.Contains(p.Id)))
        {
            user.UserDatePreferences.Add(new UserDatePreference { UserId = user.Id, DatePreferenceId = preference.Id, CreatedAt = now });
        }
    }
}
