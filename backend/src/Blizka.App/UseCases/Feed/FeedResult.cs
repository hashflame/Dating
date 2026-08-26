using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Feed;

/// <summary>Результат <see cref="GetFeedQuery"/> (T-5.1).</summary>
/// <param name="Items">Карточки, отсортированные по убыванию совместимости.</param>
/// <param name="Exhausted">Кандидаты в этом городе закончились — не значит, что <paramref name="Items"/> пуст в моменте, а что пул для повторных запросов исчерпан.</param>
/// <param name="RemainingToday">Сколько свайпов осталось до дневного лимита (spec 002, B3).</param>
public sealed record FeedResult(IReadOnlyList<FeedCardResult> Items, bool Exhausted, int RemainingToday);

/// <summary>Карточка анкеты в ленте (T-5.1, S-10/S-11) — полный набор данных для шторки на клиенте.</summary>
public sealed record FeedCardResult(
    Guid UserId,
    string Name,
    int Age,
    string? Bio,
    string CityName,
    double? DistanceKm,
    IReadOnlyList<FeedPhotoResult> Photos,
    IReadOnlyList<FeedInterestResult> Interests,
    IReadOnlyList<string> Prompts,
    bool IsVerified,
    int CompatibilityScore,
    bool DatingGoalMatch,
    int SharedInterestsCount,
    bool BothVerified,
    int SharedDatePreferencesCount,
    DatingGoal? DatingGoal,
    DateTimeOffset? LastActive);

public sealed record FeedPhotoResult(Guid Id, string Url, string ThumbnailUrl, string MediumUrl, bool IsMain);

public sealed record FeedInterestResult(Guid Id, string Name, bool IsMatch);
