using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Onboarding;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Users;

/// <summary>
/// Частично обновляет профиль текущего пользователя (T-9.1), пересчитывает ProfileCompleteness и
/// начисляет бонус за впервые достигнутый порог (60/80/100%) — тот же <see cref="ProfileCompletenessBonusAwarder"/>,
/// что и при завершении онбординга (T-2.3).
/// </summary>
public sealed class PatchUserProfileCommandHandler(
    IUserRepository userRepository,
    IUserDatePreferenceRepository datePreferenceRepository,
    ISparksService sparksService,
    IValidator<PatchUserProfileCommand> validator,
    IOptions<SparksOptions> sparksOptions)
    : IRequestHandler<PatchUserProfileCommand, PatchUserProfileResult>
{
    public async Task<PatchUserProfileResult> Handle(PatchUserProfileCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await userRepository.GetByIdWithProfileDataAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        ApplyPatch(user, request);
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
            // Параллельный PATCH того же пользователя успел сохраниться первым (xmin-конфликт) — переигрывание
            // задвоило бы начисление порогового бонуса, поэтому просто просим клиента повторить запрос, по тому
            // же принципу, что и в остальных хендлерах, сохраняющих User (см. CompleteOnboardingCommandHandler).
            throw new ProfileUpdateConflictException(request.UserId, ex);
        }

        var nextReward = ProfileCompletenessCalculator.NextReward(
            user.ProfileCompleteness, request.Locale, sparksOptions.Value.ProfileCompletionThresholdBonusAmount);

        return new PatchUserProfileResult(
            UserProfileMapper.ToResult(user, user.ProfileCompleteness, nextReward, request.Locale), sparksAwarded);
    }

    private static void ApplyPatch(User user, PatchUserProfileCommand request)
    {
        if (request.Name is { } name)
        {
            user.Name = name;
        }

        if (request.Bio is { } bio)
        {
            user.Bio = bio;
        }

        if (request.Height is { } height)
        {
            user.Height = height;
        }

        if (request.Smoking is { } smoking)
        {
            user.Smoking = smoking;
        }

        if (request.Drinking is { } drinking)
        {
            user.Drinking = drinking;
        }

        if (request.Chronotype is { } chronotype)
        {
            user.Chronotype = chronotype;
        }

        if (request.Prompts is not null)
        {
            user.Prompts = [.. request.Prompts];
        }

        if (request.DatingGoals is not null)
        {
            user.DatingGoals = [.. request.DatingGoals];
        }
    }
}
