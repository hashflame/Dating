using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Users;

public sealed record GetMeQuery(Guid UserId) : IRequest<GetMeResult>;

/// <summary>
/// Минимальный профиль текущего пользователя — id/telegramId/имя/баланс зорок/статус для главного экрана
/// (нужен клиенту, чтобы показать баланс зорок и понять, завершён ли онбординг). Не путать с полным профилем
/// T-9.1 (bio, completeness, nextReward и т.д.) — та задача ещё не реализована.
/// </summary>
public sealed record GetMeResult(
    Guid Id, long TelegramId, string Name, int SparksBalance, UserStatus Status, string Locale);
