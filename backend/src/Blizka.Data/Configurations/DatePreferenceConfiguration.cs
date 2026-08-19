using Blizka.App.Domain.Entities;
using Blizka.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class DatePreferenceConfiguration : IEntityTypeConfiguration<DatePreference>
{
    public void Configure(EntityTypeBuilder<DatePreference> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code).HasConversion<string>();

        builder.HasIndex(d => d.Code).IsUnique();

        builder.HasData(DatePreferenceSeed.All);
    }
}
