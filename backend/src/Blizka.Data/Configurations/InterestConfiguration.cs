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

        // По образцу CityConfiguration (T-4.1) — тот же trigram-поиск, T-9.2. Имя для NameRu задано явно
        // (а не оставлено дефолтным "IX_Interests_NameRu"), потому что ниже на том же столбце есть второй,
        // уникальный индекс — без разных явных имён с самого начала EF Core схлопывает оба HasIndex-вызова
        // над одинаковым списком свойств в один построитель, и итоговый индекс получается одновременно
        // GIN и UNIQUE, а GIN не поддерживает уникальность (ошибка Postgres при применении миграции).
        builder.HasIndex(i => i.NameRu, "IX_Interests_NameRu").HasMethod("GIN").HasOperators("gin_trgm_ops");
        builder.HasIndex(i => i.NameBe).HasMethod("GIN").HasOperators("gin_trgm_ops");
        builder.HasIndex(i => i.NameEn).HasMethod("GIN").HasOperators("gin_trgm_ops");

        // Точное (без учёта регистра дедуп на уровне приложения — IInterestRepository.FindByNameAsync) —
        // без обычного B-tree уникального индекса два параллельных PATCH с одинаковым новым кастомным
        // названием создали бы дубликат Interest (T-9.2).
        builder.HasIndex(i => i.NameRu, "IX_Interests_NameRu_Unique").IsUnique();

        builder.HasData(InterestSeed.All);
    }
}
