namespace Blizka.App.Sparks;

public sealed class SparksOptions
{
    public const string SectionName = "Sparks";

    /// <summary>Стоимость суперлайка в зорках (T-5.2). Спекой (spec.md 15.2) сумма оставлена конфигурируемой, без дефолта — ✦5 выбран как MVP-плейсхолдер.</summary>
    public int SuperlikeCost { get; set; } = 5;

    /// <summary>Стоимость разовой разблокировки входящих лайков навсегда (T-6.1, spec.md 7.2) — ✦10 задано буквально в decomposition.md.</summary>
    public int LikesRevealCost { get; set; } = 10;

    /// <summary>Стоимость открытия контакта в мэтче (T-7.1/T-7.3, decomposition.md: «Списать ✦1») — задано буквально, не MVP-плейсхолдер.</summary>
    public int ContactUnlockCost { get; set; } = 1;

    /// <summary>Регистрационный бонус при завершении онбординга (T-2.3/T-8.1, decomposition.md 15.2: «registration 50») — задано буквально.</summary>
    public int RegistrationBonusAmount { get; set; } = 50;

    /// <summary>Бонус за каждый из трёх порогов ProfileCompleteness — 60/80/100% (T-2.3/T-8.1, decomposition.md 15.2: «profile 2+2+2») — задано буквально.</summary>
    public int ProfileCompletionThresholdBonusAmount { get; set; } = 2;

    /// <summary>Бонус за верификацию по селфи (T-18.1/T-8.1, decomposition.md 15.2: «verification 3») — задано буквально; вызывающего кода пока нет, заводится под будущую T-18.1.</summary>
    public int VerificationBonusAmount { get; set; } = 3;

    /// <summary>Бонус за реферала (T-20.1/T-8.1, decomposition.md 15.2: «referral 2») — задано буквально; вызывающего кода пока нет, заводится под будущую T-20.1.</summary>
    public int ReferralBonusAmount { get; set; } = 2;

    /// <summary>Бонус за отправку идеи на доску (T-19.1/T-8.1, decomposition.md 15.2: «idea 1/10») — задано буквально; вызывающего кода пока нет, заводится под будущую T-19.1.</summary>
    public int IdeaSubmissionBonusAmount { get; set; } = 1;

    /// <summary>Бонус за реализованную идею (T-19.1/T-8.1, decomposition.md 15.2: «idea 1/10») — задано буквально; вызывающего кода пока нет, заводится под будущую T-19.1.</summary>
    public int IdeaImplementedBonusAmount { get; set; } = 10;
}
