using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class SparkTransactionConfiguration : IEntityTypeConfiguration<SparkTransaction>
{
    public void Configure(EntityTypeBuilder<SparkTransaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type).HasConversion<string>();

        builder.HasIndex(t => new { t.UserId, t.CreatedAt });

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
