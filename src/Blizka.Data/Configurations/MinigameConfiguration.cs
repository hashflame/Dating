using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class MinigameConfiguration : IEntityTypeConfiguration<Minigame>
{
    public void Configure(EntityTypeBuilder<Minigame> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.Match)
            .WithMany()
            .HasForeignKey(m => m.MatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
