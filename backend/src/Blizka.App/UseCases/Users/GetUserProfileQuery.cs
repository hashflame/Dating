using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Users;

/// <param name="RequestingUserId">Id того, кто смотрит анкету — нужен, чтобы скрыть её при взаимной блокировке
/// (см. doc-комментарий обработчика), т.к. блокировка симметрична и не привязана к <see cref="TargetUserId"/>.</param>
/// <param name="Locale">Локаль запроса — для локализации названий города/интересов, тем же принципом, что и T-5.1 (см. <c>CityLocaleResolver</c>).</param>
public sealed record GetUserProfileQuery(Guid TargetUserId, Guid RequestingUserId, string Locale) : IRequest<UserProfileResult>;

/// <summary>
/// Полная анкета произвольного пользователя (<c>GET /api/users/{userId}</c>) — тот же набор полей, что и
/// собственный превью-профиль (<see cref="ProfilePreviewResult"/>, T-9.1) и карточка ленты (<c>FeedCardResult</c>,
/// T-5.1). Нужен, чтобы открыть анкету из списков, которые отдают только урезанный набор полей —
/// <c>LikeUserDto</c> (T-6.1: userId/name/age/mainPhotoUrl) в первую очередь.
/// </summary>
public sealed record UserProfileResult(
    Guid UserId,
    string Name,
    int? Age,
    string? Bio,
    string CityName,
    IReadOnlyList<UserProfilePhotoResult> Photos,
    IReadOnlyList<UserProfileInterestResult> Interests,
    IReadOnlyList<string> Prompts,
    bool IsVerified,
    DatingGoal? DatingGoal);

public sealed record UserProfilePhotoResult(Guid Id, string Url, string ThumbnailUrl, string MediumUrl, bool IsMain);

public sealed record UserProfileInterestResult(Guid Id, string Name);
