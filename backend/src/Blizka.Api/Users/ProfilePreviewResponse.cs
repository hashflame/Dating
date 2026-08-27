using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Users;

namespace Blizka.Api.Users;

/// <summary>Ответ <c>GET /api/users/me/preview</c> (T-9.1) — профиль текущего пользователя в формате карточки ленты, как его видят другие.</summary>
public sealed record ProfilePreviewResponse(
    Guid UserId,
    string Name,
    int? Age,
    string? Bio,
    string CityName,
    ProfilePreviewPhotoDto[] Photos,
    ProfilePreviewInterestDto[] Interests,
    string[] Prompts,
    bool IsVerified,
    DatingGoal? DatingGoal)
{
    public static ProfilePreviewResponse From(ProfilePreviewResult result) => new(
        result.UserId,
        result.Name,
        result.Age,
        result.Bio,
        result.CityName,
        result.Photos.Select(p => new ProfilePreviewPhotoDto(p.Id, p.Url, p.ThumbnailUrl, p.MediumUrl, p.IsMain)).ToArray(),
        result.Interests.Select(i => new ProfilePreviewInterestDto(i.Id, i.Name)).ToArray(),
        [.. result.Prompts],
        result.IsVerified,
        result.DatingGoal);
}

public sealed record ProfilePreviewPhotoDto(Guid Id, string Url, string ThumbnailUrl, string MediumUrl, bool IsMain);

public sealed record ProfilePreviewInterestDto(Guid Id, string Name);
