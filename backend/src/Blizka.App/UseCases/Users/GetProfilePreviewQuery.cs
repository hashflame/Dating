using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Users;

/// <param name="Locale">Локаль запроса — для локализации названий города/интересов, тем же принципом, что и T-5.1 (см. <c>CityLocaleResolver</c>).</param>
public sealed record GetProfilePreviewQuery(Guid UserId, string Locale) : IRequest<ProfilePreviewResult>;

/// <summary>
/// Профиль текущего пользователя "как видят другие" (T-9.1, <c>GET /api/users/me/preview</c>) — тот же
/// набор полей, что и карточка ленты (<c>FeedCardResult</c>, T-5.1), без полей, которые нет смысла
/// показывать самому пользователю его же профилю (расстояние до себя, совместимость с самим собой).
/// </summary>
public sealed record ProfilePreviewResult(
    Guid UserId,
    string Name,
    int? Age,
    string? Bio,
    string CityName,
    IReadOnlyList<ProfilePreviewPhotoResult> Photos,
    IReadOnlyList<ProfilePreviewInterestResult> Interests,
    IReadOnlyList<string> Prompts,
    bool IsVerified,
    DatingGoal? DatingGoal);

public sealed record ProfilePreviewPhotoResult(Guid Id, string Url, string ThumbnailUrl, string MediumUrl, bool IsMain);

public sealed record ProfilePreviewInterestResult(Guid Id, string Name);
