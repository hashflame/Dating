using Blizka.App.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Blizka.App.Domain.Repositories;

/// <summary>
/// Критерии подбора кандидатов ленты (T-5.4) для <see cref="IFeedRepository.GetCandidatesAsync"/> — либо
/// персистентный <c>UserFilter</c> пользователя, либо MVP-дефолты, если он ещё ничего не сохранял.
/// </summary>
/// <param name="PreferredGender"><c>null</c> — пол не фильтруется (соответствует <see cref="ShowGenderPreference.All"/>).</param>
/// <param name="OriginCoordinates">Точка отсчёта расстояния — координаты текущего пользователя либо его города.</param>
/// <param name="MaxDistanceMeters">Радиус подбора (T-5.4 заменил строгое совпадение города, T-5.1).</param>
/// <param name="AgeMin">Нижняя граница возраста включительно, если задана.</param>
/// <param name="AgeMax">Верхняя граница возраста включительно, если задана.</param>
/// <param name="DatingGoals">Пусто/<c>null</c> — цель знакомств не сужает выборку.</param>
/// <param name="ActiveWithinDays">Кандидат должен был заходить за последние N дней, если задано.</param>
public sealed record FeedCandidateFilter(
    Gender? PreferredGender,
    Point OriginCoordinates,
    double MaxDistanceMeters,
    int? AgeMin,
    int? AgeMax,
    IReadOnlyCollection<DatingGoal>? DatingGoals,
    bool RequireFilledProfile,
    int? ActiveWithinDays,
    bool RequirePhoto,
    bool VerifiedOnly,
    bool NonSmoker,
    bool NonDrinker,
    bool NoChildren);
