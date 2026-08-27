namespace Blizka.App.Domain.Enums;

public enum SparkTransactionType
{
    RegistrationBonus,
    ProfileCompletion,
    Verification,
    Referral,
    IdeaSubmission,
    IdeaImplemented,
    ContactUnlock,
    Superlike,
    LikesReveal,
    Purchase,
    Refund,

    /// <summary>Корректировка баланса dev-инструментом <c>POST /api/dev/reset-my-state</c> (не начисление/списание по продуктовому сценарию).</summary>
    DevReset,

    /// <summary>Корректировка баланса при повторном входе на ранее удалённый аккаунт (<c>AuthenticateTelegramUserCommandHandler</c>) — не начисление/списание по продуктовому сценарию.</summary>
    AccountRevival,
}
