using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class IdeaConfiguration : IEntityTypeConfiguration<Idea>
{
    public void Configure(EntityTypeBuilder<Idea> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Status).HasConversion<string>();
        builder.Property(i => i.Text).IsRequired();

        builder.HasOne(i => i.AuthorUser)
            .WithMany()
            .HasForeignKey(i => i.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
