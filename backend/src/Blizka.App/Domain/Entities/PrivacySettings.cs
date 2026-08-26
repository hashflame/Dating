namespace Blizka.App.Domain.Entities;

/// <summary>
/// Настройки приватности пользователя (T-16.1, S-51) — одна строка на пользователя, создаётся лениво при
/// первом <c>PATCH /api/privacy/settings</c> (не при регистрации), поэтому её отсутствие в БД равносильно
/// значениям по умолчанию, а не ошибке (см. <see cref="Blizka.App.UseCases.Privacy.PrivacySettingsDefaults"/>).
/// </summary>
public sealed class PrivacySettings
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    /// <summary>«Запретить писать мне первой» (S-32) — контакт не открывается другими, username не показывается, contactStatus = writes_first_only.</summary>
    public bool BlockIncomingMessages { get; set; }

    /// <summary>Скрывает точную дистанцию в ленте — виден только город.</summary>
    public bool HideDistance { get; set; }

    /// <summary>Скрывает возраст в ленте.</summary>
    public bool HideAge { get; set; }

    /// <summary>Показывать ли «был(а) недавно» другим пользователям — включено по умолчанию, в отличие от остальных полей.</summary>
    public bool ShowLastActive { get; set; } = true;

    /// <summary>Невидимый режим — доступен только подписчикам «Безлимит» (T-8.3), проверяется при включении.</summary>
    public bool InvisibleMode { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
