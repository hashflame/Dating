using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class PrivacySettingsConfiguration : IEntityTypeConfiguration<PrivacySettings>
{
    public void Configure(EntityTypeBuilder<PrivacySettings> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.UserId).IsUnique();

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
