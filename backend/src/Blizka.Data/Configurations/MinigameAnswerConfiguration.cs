using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class MinigameAnswerConfiguration : IEntityTypeConfiguration<MinigameAnswer>
{
    public void Configure(EntityTypeBuilder<MinigameAnswer> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => new { a.MinigameId, a.UserId, a.DilemmaIndex }).IsUnique();

        builder.HasOne(a => a.Minigame)
            .WithMany()
            .HasForeignKey(a => a.MinigameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
