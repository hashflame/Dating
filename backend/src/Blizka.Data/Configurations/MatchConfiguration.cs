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

        builder.HasIndex(m => new { m.User1Id, m.User2Id }).IsUnique();

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
    }
}
