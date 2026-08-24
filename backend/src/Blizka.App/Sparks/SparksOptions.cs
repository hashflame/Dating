namespace Blizka.App.Sparks;

public sealed class SparksOptions
{
    public const string SectionName = "Sparks";

    /// <summary>Стоимость суперлайка в зорках (T-5.2). Спекой (spec.md 15.2) сумма оставлена конфигурируемой, без дефолта — ✦5 выбран как MVP-плейсхолдер.</summary>
    public int SuperlikeCost { get; set; } = 5;

    /// <summary>Стоимость разовой разблокировки входящих лайков навсегда (T-6.1, spec.md 7.2) — ✦10 задано буквально в decomposition.md.</summary>
    public int LikesRevealCost { get; set; } = 10;
}
