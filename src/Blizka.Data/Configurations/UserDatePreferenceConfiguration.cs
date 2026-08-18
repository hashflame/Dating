using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class UserDatePreferenceConfiguration : IEntityTypeConfiguration<UserDatePreference>
{
    public void Configure(EntityTypeBuilder<UserDatePreference> builder)
    {
        builder.HasKey(p => new { p.UserId, p.DatePreferenceId });

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.DatePreference)
            .WithMany(d => d.UserDatePreferences)
            .HasForeignKey(p => p.DatePreferenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
