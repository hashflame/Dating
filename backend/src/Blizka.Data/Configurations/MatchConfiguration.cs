using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.HasKey(m => m.Id);

        // Оптимистичная блокировка по системной колонке Postgres xmin — тот же приём, что и у User
        // (UserConfiguration). Защищает от гонки, когда оба участника мэтча почти одновременно вызывают
        // POST /unlock (T-7.3): без токена конкурентности второй SaveChangesAsync тихо перезаписывает
        // ContactUnlockedAt/ContactUnlockedByUserId первого, и оба пользователя списывают зорки за одно и
        // то же открытие контакта вместо одного.
        builder.Property<uint>("xmin").IsRowVersion();

        builder.Property(m => m.Status).HasConversion<string>();

        builder.Property(m => m.ArchivedReason).HasMaxLength(32);

        builder.HasIndex(m => new { m.User1Id, m.User2Id }).IsUnique();

        // Под предикат ArchiveStaleMatchesJob (T-7.4, раз в 6 часов) — без индекса это full scan таблицы Match
        // на каждый прогон. Партиция по Status = Active достаточна: Archived-строки джобу больше не интересуют,
        // а ContactUnlockedAt/MatchedAt/MessageSentCheckAt внутри Active-подмножества уже отсеиваются дёшево.
        builder.HasIndex(m => m.Status).HasFilter("\"Status\" = 'Active'");

        builder.HasOne(m => m.User1)
            .WithMany()
            .HasForeignKey(m => m.User1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.User2)
            .WithMany()
            .HasForeignKey(m => m.User2Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.ContactUnlockedByUser)
            .WithMany()
            .HasForeignKey(m => m.ContactUnlockedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.DateConfirmedByUser)
            .WithMany()
            .HasForeignKey(m => m.DateConfirmedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
