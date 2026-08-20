using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Cities;
using FluentValidation;
using MediatR;

namespace Blizka.App.UseCases.Feed;

/// <summary>
/// Обрабатывает <see cref="GetFeedQuery"/> (T-5.1): собирает пул кандидатов через <see cref="IFeedRepository"/>,
/// считает совместимость (<see cref="FeedCompatibilityScorer"/>) и возвращает top-<c>Limit</c> карточек.
/// </summary>
public sealed class GetFeedQueryHandler(IFeedRepository feedRepository, IValidator<GetFeedQuery> validator)
    : IRequestHandler<GetFeedQuery, FeedResult>
{
    // Пул кандидатов, из которого выбирается top-N по совместимости — не вся таблица города разом (может
    // быть большой), но заметно больше типичного limit, чтобы скоринг было из чего выбирать.
    private const int CandidatePoolSize = 200;

    public async Task<FeedResult> Handle(GetFeedQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var currentUser = await feedRepository.GetCurrentUserAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        // Пользователь без города (онбординг ещё не пройден до конца, либо T-2.1 черновик не сохранил город) —
        // кандидатов подобрать не из чего, лента пуста и исчерпана.
        if (currentUser.CityId is null)
        {
            return new FeedResult([], Exhausted: true);
        }

        // MVP-упрощение: Gender — Male/Female, ориентация/предпочтение пола отдельно не хранится (T-5.4
        // UserFilter появится позже) — показываем противоположный пол по умолчанию.
        var preferredGender = currentUser.Gender == Gender.Male ? Gender.Female : Gender.Male;

        var candidates = await feedRepository.GetCandidatesAsync(
            currentUser.Id, currentUser.CityId.Value, preferredGender, CandidatePoolSize, cancellationToken);

        if (candidates.Count == 0)
        {
            return new FeedResult([], Exhausted: true);
        }

        var locale = CityLocaleResolver.Resolve(currentUser.Locale);
        var currentUserInterestIds = currentUser.UserInterests.Select(ui => ui.InterestId).ToHashSet();

        var items = candidates
            .Select(candidate => FeedCompatibilityScorer.Score(currentUser, candidate, currentUserInterestIds))
            .OrderByDescending(scored => scored.Score)
            .Take(request.Limit)
            .Select(scored => ToCardResult(scored, locale))
            .ToList();

        return new FeedResult(items, Exhausted: false);
    }

    private static FeedCardResult ToCardResult(ScoredCandidate scored, CityLocale locale)
    {
        var candidate = scored.Candidate;

        var photos = candidate.Photos
            .OrderBy(p => p.SortOrder)
            .Select(p => new FeedPhotoResult(p.Id, p.Url, p.ThumbnailUrl, p.MediumUrl, p.IsMain))
            .ToList();

        var interests = candidate.UserInterests
            .Where(ui => ui.Interest is not null)
            .Select(ui => new FeedInterestResult(
                ui.InterestId, InterestNameResolver.Resolve(ui.Interest!, locale), scored.SharedInterestIds.Contains(ui.InterestId)))
            .ToList();

        var cityName = candidate.City is null ? string.Empty : CityNameResolver.Resolve(candidate.City, locale);
        var age = CalculateAge(candidate.BirthDate);

        return new FeedCardResult(
            candidate.Id,
            candidate.Name,
            age,
            candidate.Bio,
            cityName,
            scored.DistanceKm,
            photos,
            interests,
            candidate.Prompts,
            candidate.IsVerified,
            scored.Score,
            scored.DatingGoalMatch,
            scored.SharedInterestIds.Count,
            scored.BothVerified);
    }

    private static int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
        var age = today.Year - birthDate.Year;
        if (today < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }
}
