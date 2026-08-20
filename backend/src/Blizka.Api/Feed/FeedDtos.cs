using Blizka.App.UseCases.Feed;

namespace Blizka.Api.Feed;

/// <summary>Ответ <c>GET /api/feed</c> (T-5.1).</summary>
/// <param name="Items">Карточки, отсортированные по убыванию совместимости.</param>
/// <param name="Exhausted">Кандидаты в городе пользователя закончились.</param>
public sealed record FeedResponse(FeedCardDto[] Items, bool Exhausted)
{
    public static FeedResponse From(FeedResult result) =>
        new(result.Items.Select(FeedCardDto.From).ToArray(), result.Exhausted);
}

/// <summary>Карточка анкеты в ленте (S-10/S-11).</summary>
public sealed record FeedCardDto(
    Guid UserId,
    string Name,
    int Age,
    string? Bio,
    string CityName,
    double? DistanceKm,
    FeedPhotoDto[] Photos,
    FeedInterestDto[] Interests,
    string[] Prompts,
    bool IsVerified,
    int CompatibilityScore,
    FeedCompatibilitySummaryDto CompatibilitySummary)
{
    public static FeedCardDto From(FeedCardResult result) => new(
        result.UserId,
        result.Name,
        result.Age,
        result.Bio,
        result.CityName,
        result.DistanceKm,
        result.Photos.Select(FeedPhotoDto.From).ToArray(),
        result.Interests.Select(FeedInterestDto.From).ToArray(),
        [.. result.Prompts],
        result.IsVerified,
        result.CompatibilityScore,
        new FeedCompatibilitySummaryDto(result.DatingGoalMatch, result.SharedInterestsCount, result.BothVerified));
}

public sealed record FeedPhotoDto(Guid Id, string Url, string ThumbnailUrl, string MediumUrl, bool IsMain)
{
    public static FeedPhotoDto From(FeedPhotoResult result) =>
        new(result.Id, result.Url, result.ThumbnailUrl, result.MediumUrl, result.IsMain);
}

/// <param name="Id">Id интереса из каталога.</param>
/// <param name="Name">Название интереса на локали текущего пользователя.</param>
/// <param name="IsMatch">Совпадает ли интерес с интересами текущего пользователя.</param>
public sealed record FeedInterestDto(Guid Id, string Name, bool IsMatch)
{
    public static FeedInterestDto From(FeedInterestResult result) => new(result.Id, result.Name, result.IsMatch);
}

/// <summary>Разбор compatibilityScore на факторы — для шторки с деталями совпадения (S-11).</summary>
public sealed record FeedCompatibilitySummaryDto(bool DatingGoalMatch, int SharedInterestsCount, bool BothVerified);
