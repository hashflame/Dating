using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Type).HasConversion<string>();
        builder.Property(c => c.Version).IsRequired().HasMaxLength(32);
        builder.Property(c => c.IpAddress).HasMaxLength(64);

        builder.HasIndex(c => new { c.UserId, c.Type });

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
