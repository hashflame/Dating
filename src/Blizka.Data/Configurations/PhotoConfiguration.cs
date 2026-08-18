using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Url).IsRequired();
        builder.Property(p => p.ThumbnailUrl).IsRequired();
        builder.Property(p => p.MediumUrl).IsRequired();

        builder.HasIndex(p => new { p.UserId, p.SortOrder });
    }
}
