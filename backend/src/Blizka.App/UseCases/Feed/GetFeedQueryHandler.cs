using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Cities;
using Blizka.App.UseCases.Swipes;
using FluentValidation;
using MediatR;
using NetTopologySuite.Geometries;

namespace Blizka.App.UseCases.Feed;

/// <summary>
/// Обрабатывает <see cref="GetFeedQuery"/> (T-5.1, T-5.4): собирает пул кандидатов через <see cref="IFeedRepository"/>
/// (с учётом сохранённого <c>UserFilter</c> либо MVP-дефолтов), считает совместимость
/// (<see cref="FeedCompatibilityScorer"/>) и возвращает top-<c>Limit</c> карточек.
/// </summary>
public sealed class GetFeedQueryHandler(
    IFeedRepository feedRepository,
    IUserFilterRepository filterRepository,
    ISwipeRepository swipeRepository,
    IValidator<GetFeedQuery> validator)
    : IRequestHandler<GetFeedQuery, FeedResult>
{
    // Пул кандидатов, из которого выбирается top-N по совместимости — не вся таблица радиуса разом (может
    // быть большой), но заметно больше типичного limit, чтобы скоринг было из чего выбирать.
    private const int CandidatePoolSize = 200;

    public async Task<FeedResult> Handle(GetFeedQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var currentUser = await feedRepository.GetCurrentUserAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        var remainingToday = await GetRemainingSwipesTodayAsync(currentUser.Id, cancellationToken);

        // Пользователь без города (онбординг ещё не пройден до конца, либо T-2.1 черновик не сохранил город) —
        // кандидатов подобрать не из чего, лента пуста и исчерпана.
        if (currentUser.CityId is null)
        {
            return new FeedResult([], Exhausted: true, remainingToday);
        }

        // Источник координат для радиуса (T-5.4, заменил строгое совпадение города из T-5.1) — своя геолокация,
        // а при её отсутствии город (у Active-пользователя CityId всегда задан, а City.Coordinates не nullable,
        // так что практически этот null недостижим — подстраховка на будущее, как и в FeedCompatibilityScorer).
        var originCoordinates = currentUser.Coordinates ?? currentUser.City?.Coordinates;
        if (originCoordinates is null)
        {
            return new FeedResult([], Exhausted: true, remainingToday);
        }

        var storedFilter = await filterRepository.GetAsync(currentUser.Id, cancellationToken);
        var filter = BuildCandidateFilter(currentUser, originCoordinates, storedFilter);

        var candidates = await feedRepository.GetCandidatesAsync(
            currentUser.Id, filter, CandidatePoolSize, cancellationToken);

        if (candidates.Count == 0)
        {
            return new FeedResult([], Exhausted: true, remainingToday);
        }

        var locale = CityLocaleResolver.Resolve(currentUser.Locale);
        var currentUserInterestIds = currentUser.UserInterests.Select(ui => ui.InterestId).ToHashSet();

        var items = candidates
            .Select(candidate => FeedCompatibilityScorer.Score(currentUser, candidate, currentUserInterestIds))
            .OrderByDescending(scored => scored.Score)
            .Take(request.Limit)
            .Select(scored => ToCardResult(scored, locale))
            .ToList();

        return new FeedResult(items, Exhausted: false, remainingToday);
    }

    private async Task<int> GetRemainingSwipesTodayAsync(Guid userId, CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddHours(-24);
        var usedToday = await swipeRepository.CountSinceAsync(userId, since, cancellationToken);
        return Math.Max(0, SwipeLimits.DailyLimit - usedToday);
    }

    private static FeedCandidateFilter BuildCandidateFilter(
        User currentUser, Point originCoordinates, UserFilter? storedFilter)
    {
        var preferredGender = ResolvePreferredGender(currentUser.Gender, storedFilter?.ShowGender);
        var maxDistanceKm = storedFilter?.MaxDistanceKm ?? UserFilterDefaults.MaxDistanceKm;

        return new FeedCandidateFilter(
            preferredGender,
            originCoordinates,
            MaxDistanceMeters: maxDistanceKm * 1000.0,
            AgeMin: storedFilter?.AgeMin,
            AgeMax: storedFilter?.AgeMax,
            DatingGoals: storedFilter?.DatingGoals,
            RequireFilledProfile: storedFilter?.RequireFilledProfile ?? false,
            ActiveWithinDays: storedFilter?.ActiveWithinDays,
            RequirePhoto: storedFilter?.RequirePhoto ?? UserFilterDefaults.RequirePhoto,
            VerifiedOnly: storedFilter?.VerifiedOnly ?? UserFilterDefaults.VerifiedOnly,
            NonSmoker: storedFilter?.NonSmoker ?? false,
            NonDrinker: storedFilter?.NonDrinker ?? false,
            NoChildren: storedFilter?.NoChildren ?? false);
    }

    private static Gender? ResolvePreferredGender(Gender ownGender, ShowGenderPreference? showGender) => showGender switch
    {
        ShowGenderPreference.Male => Gender.Male,
        ShowGenderPreference.Female => Gender.Female,
        ShowGenderPreference.All => null,
        // Нет сохранённого UserFilter — MVP-дефолт: показываем противоположный пол (T-5.1, до T-5.4).
        null => ownGender == Gender.Male ? Gender.Female : Gender.Male,
        _ => throw new ArgumentOutOfRangeException(nameof(showGender), showGender, "Unknown ShowGenderPreference."),
    };

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
            scored.BothVerified,
            candidate.DatingGoal,
            candidate.LastActiveAt);
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
