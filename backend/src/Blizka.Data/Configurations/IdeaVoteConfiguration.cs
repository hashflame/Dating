using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class IdeaVoteConfiguration : IEntityTypeConfiguration<IdeaVote>
{
    public void Configure(EntityTypeBuilder<IdeaVote> builder)
    {
        builder.HasKey(v => new { v.IdeaId, v.UserId });

        builder.HasOne(v => v.Idea)
            .WithMany(i => i.Votes)
            .HasForeignKey(v => v.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
