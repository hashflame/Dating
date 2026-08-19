using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class TelegramPaymentConfiguration : IEntityTypeConfiguration<TelegramPayment>
{
    public void Configure(EntityTypeBuilder<TelegramPayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status).HasConversion<string>();
        builder.Property(p => p.TelegramPaymentChargeId).IsRequired();

        builder.HasIndex(p => p.TelegramPaymentChargeId).IsUnique();

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
