using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.HasKey(b => b.Id);

        builder.HasIndex(b => new { b.BlockerUserId, b.BlockedUserId }).IsUnique();
        builder.HasIndex(b => b.BlockedUserId);

        builder.HasOne(b => b.BlockerUser)
            .WithMany()
            .HasForeignKey(b => b.BlockerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.BlockedUser)
            .WithMany()
            .HasForeignKey(b => b.BlockedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
