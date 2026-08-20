using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Blizka.App.UseCases.Feed;

/// <summary>
/// Обрабатывает <see cref="PatchFeedFiltersCommand"/> (T-5.4): создаёт <c>UserFilter</c> при первом
/// сохранении (с MVP-дефолтами для полей, не присланных в этом запросе) либо частично обновляет уже
/// существующий — по тому же паттерну "load-or-create", что и <c>PatchOnboardingDraftCommandHandler</c>.
/// </summary>
public sealed class PatchFeedFiltersCommandHandler(
    IUserFilterRepository filterRepository,
    IUserRepository userRepository,
    IValidator<PatchFeedFiltersCommand> validator)
    : IRequestHandler<PatchFeedFiltersCommand, FeedFiltersResult>
{
    public async Task<FeedFiltersResult> Handle(PatchFeedFiltersCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var filter = await filterRepository.GetAsync(request.UserId, cancellationToken);
        var isNewFilter = filter is null;

        if (isNewFilter)
        {
            filter = await CreateWithDefaultsAsync(request.UserId, cancellationToken);
        }

        ApplyPatch(filter!, request);

        if (isNewFilter)
        {
            await filterRepository.AddAsync(filter!, cancellationToken);
        }

        try
        {
            await filterRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUserFilterCreationException) when (isNewFilter)
        {
            // Параллельный PATCH того же пользователя успел создать фильтр первым — подхватываем уже
            // созданную запись и накладываем на неё наши данные вместо падения в 500.
            filter = await filterRepository.GetAsync(request.UserId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"UserFilter for user {request.UserId} not found after a concurrent-creation conflict.");

            ApplyPatch(filter, request);
            await filterRepository.SaveChangesAsync(cancellationToken);
        }

        return FeedFiltersResult.From(filter!);
    }

    private async Task<UserFilter> CreateWithDefaultsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {userId} not found.");

        return new UserFilter
        {
            UserId = userId,
            ShowGender = UserFilterDefaults.ResolveDefaultShowGender(user.Gender),
            AgeMin = UserFilterDefaults.AgeMin,
            AgeMax = UserFilterDefaults.AgeMax,
            MaxDistanceKm = UserFilterDefaults.MaxDistanceKm,
            DatingGoals = [],
        };
    }

    private static void ApplyPatch(UserFilter filter, PatchFeedFiltersCommand request)
    {
        if (request.ShowGender is { } showGender)
        {
            filter.ShowGender = showGender;
        }

        if (request.AgeRange is { } ageRange)
        {
            filter.AgeMin = ageRange.Min;
            filter.AgeMax = ageRange.Max;
        }

        if (request.MaxDistanceKm is { } maxDistanceKm)
        {
            filter.MaxDistanceKm = maxDistanceKm;
        }

        if (request.DatingGoals is not null)
        {
            filter.DatingGoals = [.. request.DatingGoals];
        }

        if (request.RequireFilledProfile is { } requireFilledProfile)
        {
            filter.RequireFilledProfile = requireFilledProfile;
        }

        if (request.ActiveWithinDays is { } activeWithinDays)
        {
            filter.ActiveWithinDays = activeWithinDays == PatchFeedFiltersCommand.ClearActiveWithinDays
                ? null
                : activeWithinDays;
        }

        if (request.RequirePhoto is { } requirePhoto)
        {
            filter.RequirePhoto = requirePhoto;
        }

        if (request.VerifiedOnly is { } verifiedOnly)
        {
            filter.VerifiedOnly = verifiedOnly;
        }

        if (request.NonSmoker is { } nonSmoker)
        {
            filter.NonSmoker = nonSmoker;
        }

        if (request.NonDrinker is { } nonDrinker)
        {
            filter.NonDrinker = nonDrinker;
        }

        if (request.NoChildren is { } noChildren)
        {
            filter.NoChildren = noChildren;
        }

        filter.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
