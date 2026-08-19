namespace Blizka.App.UseCases.Photos;

public sealed record PhotoResult(
    Guid Id,
    string Url,
    string ThumbnailUrl,
    string MediumUrl,
    int SortOrder,
    bool IsMain,
    DateTimeOffset CreatedAt);
