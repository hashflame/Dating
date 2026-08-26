using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Entities;

/// <summary>
/// Реферальная связь (T-20.1): создаётся при регистрации приглашённого по ссылке вида
/// <c>https://t.me/{bot}?start=ref_{code}</c> (см. <see cref="Blizka.App.Referrals.ReferralCodeCodec"/>),
/// переводится в <see cref="ReferralStatus.Completed"/> при завершении его онбординга — тогда же рефереру
/// начисляется бонус (<c>SparksOptions.ReferralBonusAmount</c>) через <see cref="SparkTransactionType.Referral"/>.
/// </summary>
public sealed class Referral
{
    public Guid Id { get; set; }

    public Guid ReferrerUserId { get; set; }

    public User? ReferrerUser { get; set; }

    public Guid ReferredUserId { get; set; }

    public User? ReferredUser { get; set; }

    /// <summary>Код из start_param на момент регистрации приглашённого — хранится для аудита (сама привязка выполняется по ReferrerUserId/ReferredUserId).</summary>
    public string Code { get; set; } = string.Empty;

    public ReferralStatus Status { get; set; } = ReferralStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
