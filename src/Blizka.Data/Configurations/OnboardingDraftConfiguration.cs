using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class OnboardingDraftConfiguration : IEntityTypeConfiguration<OnboardingDraft>
{
    public void Configure(EntityTypeBuilder<OnboardingDraft> builder)
    {
        builder.HasKey(d => d.UserId);

        builder.Property(d => d.DataJson).HasColumnType("jsonb").IsRequired();

        builder.HasOne(d => d.User)
            .WithOne()
            .HasForeignKey<OnboardingDraft>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
