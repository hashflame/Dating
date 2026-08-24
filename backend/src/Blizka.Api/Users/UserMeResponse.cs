using Blizka.App.Domain.Enums;

namespace Blizka.Api.Users;

/// <summary>Минимальный профиль текущего пользователя (T-8.1/T-1.1) — id, telegramId, имя, баланс зорок, статус.</summary>
/// <param name="Id">Id пользователя.</param>
/// <param name="TelegramId">Telegram id пользователя.</param>
/// <param name="Name">Имя из онбординга.</param>
/// <param name="SparksBalance">Текущий баланс зорок.</param>
/// <param name="Status">Статус аккаунта — по нему клиент определяет, завершён ли онбординг (<c>New</c>/<c>Onboarding</c> — не завершён).</param>
/// <param name="Locale">Локаль пользователя.</param>
public sealed record UserMeResponse(
    Guid Id, long TelegramId, string Name, int SparksBalance, UserStatus Status, string Locale);
