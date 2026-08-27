using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Users;

namespace Blizka.Api.Users;

/// <summary>Ответ <c>GET /api/users/{userId}</c> — полная анкета произвольного пользователя (открыть из списка).</summary>
public sealed record UserProfileResponse(
    Guid UserId,
    string Name,
    int? Age,
    string? Bio,
    string CityName,
    UserProfilePhotoDto[] Photos,
    UserProfileInterestDto[] Interests,
    string[] Prompts,
    bool IsVerified,
    DatingGoal? DatingGoal)
{
    public static UserProfileResponse From(UserProfileResult result) => new(
        result.UserId,
        result.Name,
        result.Age,
        result.Bio,
        result.CityName,
        result.Photos.Select(p => new UserProfilePhotoDto(p.Id, p.Url, p.ThumbnailUrl, p.MediumUrl, p.IsMain)).ToArray(),
        result.Interests.Select(i => new UserProfileInterestDto(i.Id, i.Name)).ToArray(),
        [.. result.Prompts],
        result.IsVerified,
        result.DatingGoal);
}

public sealed record UserProfilePhotoDto(Guid Id, string Url, string ThumbnailUrl, string MediumUrl, bool IsMain);

public sealed record UserProfileInterestDto(Guid Id, string Name);
