using Blizka.App.Domain.Entities;
using Blizka.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class InterestConfiguration : IEntityTypeConfiguration<Interest>
{
    public void Configure(EntityTypeBuilder<Interest> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Category).HasConversion<string>();

        builder.Property(i => i.NameRu).HasMaxLength(50).IsRequired();
        builder.Property(i => i.NameBe).HasMaxLength(50).IsRequired();
        builder.Property(i => i.NameEn).HasMaxLength(50).IsRequired();

        builder.HasData(InterestSeed.All);
    }
}
