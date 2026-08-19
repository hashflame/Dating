using Blizka.App.Domain.Entities;
using Blizka.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.NameRu).HasMaxLength(100).IsRequired();
        builder.Property(c => c.NameBe).HasMaxLength(100).IsRequired();
        builder.Property(c => c.NameEn).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Country).HasMaxLength(2).IsRequired();
        builder.Property(c => c.Coordinates).HasColumnType("geography (Point, 4326)").IsRequired();

        builder.HasIndex(c => c.NameRu).HasMethod("GIN").HasOperators("gin_trgm_ops");
        builder.HasIndex(c => c.NameBe).HasMethod("GIN").HasOperators("gin_trgm_ops");
        builder.HasIndex(c => c.NameEn).HasMethod("GIN").HasOperators("gin_trgm_ops");

        builder.HasData(CitySeed.All);
    }
}
