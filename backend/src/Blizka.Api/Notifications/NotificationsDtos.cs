using Blizka.App.UseCases.Notifications;

namespace Blizka.Api.Notifications;

/// <summary>Ответ <c>GET /api/notifications/unread</c> (T-10.2).</summary>
public sealed record UnreadNotificationsResponse(int Likes, int Matches)
{
    public static UnreadNotificationsResponse From(UnreadNotificationsCountResult result) => new(result.Likes, result.Matches);
}
