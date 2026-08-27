using Blizka.App.Domain.Entities;
using NetTopologySuite.Geometries;

namespace Blizka.App.UseCases.Feed;

/// <summary>
/// Скоринг совместимости кандидата в ленте (T-5.1). Вес совпадения цели знакомств (0.15) — из заметок S-04
/// в decomposition.md; веса пересечения интересов (0.30), близости (0.30), бонуса за верификацию (0.15) и
/// совпадения предпочтений на свидания (0.10, T-9.3) спекой не заданы — выбраны как MVP-приближение (сумма
/// весов = 1.0), интересы и расстояние остаются доминирующими как более персональные сигналы, чем
/// верификация или предпочтения формата свидания.
/// </summary>
internal static class FeedCompatibilityScorer
{
    private const double DatingGoalWeight = 0.15;
    private const double InterestsWeight = 0.30;
    private const double DistanceWeight = 0.30;
    private const double VerifiedWeight = 0.15;
    private const double DatePreferencesWeight = 0.10;

    // Не линейный спад: на 20км совместимость по расстоянию падает вдвое от максимума, дальше — постепенно к нулю.
    private const double DistanceDecayKm = 20.0;

    // Координаты неизвестны хотя бы у одного (нет геолокации и город без Coordinates не бывает, но подстраховка
    // на случай будущих данных) — не наказываем и не поощряем, нейтральный вклад в общий скор.
    private const double NeutralDistanceScore = 0.5;

    private const double EarthRadiusKm = 6371.0;

    public static ScoredCandidate Score(
        User currentUser,
        User candidate,
        IReadOnlySet<Guid> currentUserInterestIds,
        IReadOnlySet<Guid> currentUserDatePreferenceIds)
    {
        // User.DatingGoal стал User.DatingGoals[] (до двух целей, как в макете S-04, тикет ClickUp) — совпадение
        // теперь по пересечению множеств, а не по равенству одиночных значений.
        var datingGoalMatch = currentUser.DatingGoals.Length > 0 && currentUser.DatingGoals.Intersect(candidate.DatingGoals).Any();

        var candidateInterestIds = candidate.UserInterests.Select(ui => ui.InterestId).ToHashSet();
        var sharedInterestIds = currentUserInterestIds.Where(candidateInterestIds.Contains).ToHashSet();
        var interestsScore = currentUserInterestIds.Count == 0
            ? 0.0
            : (double)sharedInterestIds.Count / currentUserInterestIds.Count;

        var candidateDatePreferenceIds = candidate.UserDatePreferences.Select(p => p.DatePreferenceId).ToHashSet();
        var sharedDatePreferenceIds = currentUserDatePreferenceIds.Where(candidateDatePreferenceIds.Contains).ToHashSet();
        var datePreferencesScore = currentUserDatePreferenceIds.Count == 0
            ? 0.0
            : (double)sharedDatePreferenceIds.Count / currentUserDatePreferenceIds.Count;

        var distanceKm = CalculateDistanceKm(currentUser, candidate);
        var distanceScore = distanceKm is null
            ? NeutralDistanceScore
            : DistanceDecayKm / (DistanceDecayKm + distanceKm.Value);

        var bothVerified = currentUser.IsVerified && candidate.IsVerified;

        var total =
            (datingGoalMatch ? 1.0 : 0.0) * DatingGoalWeight +
            interestsScore * InterestsWeight +
            distanceScore * DistanceWeight +
            (bothVerified ? 1.0 : 0.0) * VerifiedWeight +
            datePreferencesScore * DatePreferencesWeight;

        var scorePercent = (int)Math.Round(total * 100, MidpointRounding.AwayFromZero);

        return new ScoredCandidate(
            candidate, scorePercent, datingGoalMatch, sharedInterestIds, distanceKm, bothVerified, sharedDatePreferenceIds.Count);
    }

    /// <summary>
    /// Гаверсинус, а не <see cref="Geometry.Distance(Geometry)"/> у NTS: тот считает плоское расстояние в
    /// единицах координат (градусах) для in-memory геометрий — перевод в метры через PostGIS ST_Distance
    /// (как в <c>CityRepository</c>) происходит только при трансляции LINQ в SQL, а тут сущности уже
    /// материализованы и считаем на C#.
    /// </summary>
    private static double? CalculateDistanceKm(User currentUser, User candidate)
    {
        var from = currentUser.Coordinates ?? currentUser.City?.Coordinates;
        var to = candidate.Coordinates ?? candidate.City?.Coordinates;

        if (from is null || to is null)
        {
            return null;
        }

        var lat1 = DegreesToRadians(from.Y);
        var lat2 = DegreesToRadians(to.Y);
        var deltaLat = DegreesToRadians(to.Y - from.Y);
        var deltaLon = DegreesToRadians(to.X - from.X);

        var h = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));

        return EarthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}

/// <summary>Кандидат со скором и разложением по факторам — для маппинга в <see cref="FeedCardResult"/>.</summary>
internal sealed record ScoredCandidate(
    User Candidate,
    int Score,
    bool DatingGoalMatch,
    IReadOnlySet<Guid> SharedInterestIds,
    double? DistanceKm,
    bool BothVerified,
    int SharedDatePreferencesCount);
