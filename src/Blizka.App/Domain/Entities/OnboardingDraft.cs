namespace Blizka.App.Domain.Entities;

/// <summary>
/// Черновик анкеты онбординга (T-2.1): по одной записи на пользователя, накапливает данные всех
/// пройденных шагов в <see cref="DataJson"/>, чтобы пользователь мог продолжить с того же места.
/// </summary>
public sealed class OnboardingDraft
{
    public Guid UserId { get; set; }

    public User? User { get; set; }

    /// <summary>Номер последнего сохранённого шага (максимум из когда-либо сохранённых — не регрессирует при возврате назад).</summary>
    public int Step { get; set; }

    /// <summary>Накопленные данные всех шагов, слитые в один JSON-объект (jsonb).</summary>
    public string DataJson { get; set; } = "{}";

    public DateTimeOffset UpdatedAt { get; set; }
}
