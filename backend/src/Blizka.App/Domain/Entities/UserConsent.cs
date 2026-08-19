using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Entities;

/// <summary>
/// Запись о согласии пользователя с юридическим документом (T-2.2) — append-only лог: каждый вызов
/// <c>POST /api/users/me/consent</c> добавляет новую строку, а не перезаписывает предыдущую, чтобы
/// сохранить историю согласий (в т.ч. с более старыми версиями документа) для юридической защиты.
/// </summary>
public sealed class UserConsent
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public ConsentType Type { get; set; }

    /// <summary>Версия документа (условий использования/политики конфиденциальности), с которой согласился пользователь.</summary>
    public string Version { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    /// <summary>IP-адрес на момент согласия — для юридической доказательности; может быть неизвестен (например, в тестах).</summary>
    public string? IpAddress { get; set; }

    /// <summary>Снимок Telegram id на момент согласия (дублирует User.TelegramId, но неизменен даже если у пользователя сменится Telegram-аккаунт).</summary>
    public long TelegramId { get; set; }
}
