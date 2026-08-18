using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class QuestionOfDayConfiguration : IEntityTypeConfiguration<QuestionOfDay>
{
    public void Configure(EntityTypeBuilder<QuestionOfDay> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.TextRu).IsRequired();
        builder.Property(q => q.TextBe).IsRequired();
        builder.Property(q => q.TextEn).IsRequired();
    }
}
