using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Status).HasConversion<string>();

        builder.HasIndex(m => new { m.User1Id, m.User2Id }).IsUnique();

        builder.HasOne(m => m.User1)
            .WithMany()
            .HasForeignKey(m => m.User1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.User2)
            .WithMany()
            .HasForeignKey(m => m.User2Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.ContactUnlockedByUser)
            .WithMany()
            .HasForeignKey(m => m.ContactUnlockedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
