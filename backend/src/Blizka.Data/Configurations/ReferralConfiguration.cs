using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.HasKey(r => r.Id);

        // Один и тот же пользователь может быть приглашённым только один раз — привязка к рефереру
        // фиксируется на первой Telegram-аутентификации (AuthenticateTelegramUserCommandHandler) и дальше не меняется.
        builder.HasIndex(r => r.ReferredUserId).IsUnique();
        builder.HasIndex(r => r.ReferrerUserId);

        builder.Property(r => r.Status).HasConversion<string>();

        builder.HasOne(r => r.ReferrerUser)
            .WithMany()
            .HasForeignKey(r => r.ReferrerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReferredUser)
            .WithMany()
            .HasForeignKey(r => r.ReferredUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
