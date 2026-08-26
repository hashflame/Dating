using Blizka.App.Domain.Entities;
using Blizka.Data.Seed;
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

        // Джоба GenerateQuestionOfDay (T-11.1) публикует по одному вопросу в день — выборка следующего для
        // публикации (PublishedAt IS NULL сначала, иначе самый давно опубликованный) и текущего (максимальный
        // уже наступивший PublishedAt) идут по этому индексу.
        builder.HasIndex(q => q.PublishedAt);

        builder.HasData(QuestionOfDaySeed.All);
    }
}
