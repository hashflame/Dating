using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        // Оптимистичная блокировка по системной колонке Postgres xmin — теневое свойство, смэппленное на уже
        // существующий системный столбец (не заводит новую колонку в БД, миграция ниже это учитывает).
        // Защищает от гонки при двух параллельных обновлениях одного User (например, двойной
        // POST /api/onboarding/complete, T-2.3): второй SaveChangesAsync падает с
        // DbUpdateConcurrencyException вместо тихого перезатирания и задвоенного начисления зорок.
        builder.Property<uint>("xmin").IsRowVersion();

        builder.Property(u => u.Status).HasConversion<string>();
        builder.Property(u => u.Gender).HasConversion<string>();
        builder.Property(u => u.DatingGoal).HasConversion<string>();
        builder.Property(u => u.Smoking).HasConversion<string>();
        builder.Property(u => u.Drinking).HasConversion<string>();
        builder.Property(u => u.Chronotype).HasConversion<string>();

        builder.Property(u => u.Name).HasMaxLength(30).IsRequired();
        builder.Property(u => u.Locale).HasMaxLength(2).IsRequired();
        builder.Property(u => u.Coordinates).HasColumnType("geography (Point, 4326)");

        builder.HasIndex(u => u.TelegramId).IsUnique();

        builder.HasOne(u => u.City)
            .WithMany()
            .HasForeignKey(u => u.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Photos)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.UserInterests)
            .WithOne(ui => ui.User)
            .HasForeignKey(ui => ui.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
