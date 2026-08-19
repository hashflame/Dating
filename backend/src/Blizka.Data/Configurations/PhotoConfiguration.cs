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

        // Unique (не просто индекс для поиска) — вместе с partial-индексом ниже защищает инварианты "не больше
        // одного фото на позицию" и "не больше одного главного фото" от гонки двух параллельных загрузок
        // (см. ConcurrentPhotoUploadException).
        builder.HasIndex(p => new { p.UserId, p.SortOrder })
            .IsUnique()
            .HasDatabaseName("IX_Photos_UserId_SortOrder");

        builder.HasIndex(p => p.UserId)
            .IsUnique()
            .HasFilter("\"IsMain\" = true")
            .HasDatabaseName("IX_Photos_UserId_IsMain");
    }
}
