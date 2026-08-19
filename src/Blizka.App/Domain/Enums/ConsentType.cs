namespace Blizka.App.Domain.Enums;

/// <summary>
/// Тип юридического согласия (T-2.2). MVP-экран S-02 показывает один общий чекбокс,
/// поэтому пока единственное значение — согласие с условиями использования и политикой конфиденциальности вместе.
/// </summary>
public enum ConsentType
{
    TermsAndPrivacyPolicy,
}
