using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blizka.Data.Configurations;

public sealed class UserFilterConfiguration : IEntityTypeConfiguration<UserFilter>
{
    public void Configure(EntityTypeBuilder<UserFilter> builder)
    {
        builder.HasKey(f => f.UserId);

        builder.Property(f => f.ShowGender).HasConversion<string>();

        // DatingGoal[] хранится как text[] (тот же нативный маппинг Npgsql, что и User.Prompts) через
        // конвертер поэлементно в string[] — EF Core не умеет сам конвертировать enum-элементы внутри
        // массива через HasConversion<string>(), как для одиночных enum-колонок в этом же проекте.
        // ValueComparer обязателен для коллекционных свойств — иначе изменения массива не отслеживаются.
        builder.Property(f => f.DatingGoals)
            .HasConversion(
                goals => goals.Select(g => g.ToString()).ToArray(),
                values => values.Select(Enum.Parse<DatingGoal>).ToArray())
            .Metadata.SetValueComparer(new ValueComparer<DatingGoal[]>(
                (a, b) => a!.SequenceEqual(b!),
                a => a.Aggregate(0, (hash, g) => HashCode.Combine(hash, g)),
                a => a.ToArray()));

        builder.HasOne(f => f.User)
            .WithOne()
            .HasForeignKey<UserFilter>(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
