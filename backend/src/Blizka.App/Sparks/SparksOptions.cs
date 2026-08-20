namespace Blizka.App.Sparks;

public sealed class SparksOptions
{
    public const string SectionName = "Sparks";

    /// <summary>Стоимость суперлайка в зорках (T-5.2). Спекой (spec.md 15.2) сумма оставлена конфигурируемой, без дефолта — ✦5 выбран как MVP-плейсхолдер.</summary>
    public int SuperlikeCost { get; set; } = 5;
}
