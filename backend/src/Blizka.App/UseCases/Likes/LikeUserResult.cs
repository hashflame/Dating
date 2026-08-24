namespace Blizka.App.UseCases.Likes;

/// <summary>Пользователь-участник лайка в списках T-6.1 — общая проекция для входящих, исходящих и разблокированных.</summary>
public sealed record LikeUserResult(Guid UserId, string Name, int Age, string? MainPhotoUrl);
