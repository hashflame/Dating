using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class SwipeConfiguration : IEntityTypeConfiguration<Swipe>
{
    public void Configure(EntityTypeBuilder<Swipe> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Type).HasConversion<string>();

        // Частичный индекс — уникальность действует только для активного (не отменённого) свайпа пары.
        // T-5.3 (undo) не удаляет строку, а проставляет UndoneAt, возвращая кандидата в пул ленты (T-5.1) —
        // без фильтра повторный свайп той же пары после отмены упал бы на этот же constraint.
        builder.HasIndex(s => new { s.FromUserId, s.ToUserId })
            .IsUnique()
            .HasFilter("\"UndoneAt\" IS NULL");

        builder.HasOne(s => s.FromUser)
            .WithMany()
            .HasForeignKey(s => s.FromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ToUser)
            .WithMany()
            .HasForeignKey(s => s.ToUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
