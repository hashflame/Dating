using Blizka.App.Domain.Enums;

namespace Blizka.App.DevSeed;

/// <summary>
/// 10 фиксированных демо-анкет (спека 003) — все в Минске, детерминированные <see cref="DemoSeedUserSpec.TelegramId"/>
/// вне диапазона настоящих Telegram-id, чтобы не столкнуться с реальным пользователем.
/// </summary>
public static class DemoSeedCatalog
{
    /// <summary>Первый TelegramId зарезервированного блока (990000000001–990000000010).</summary>
    public const long TelegramIdRangeStart = 990_000_000_001;

    public static IReadOnlyList<DemoSeedUserSpec> Users { get; } = Build();

    public static bool IsDemoTelegramId(long telegramId) => Users.Any(u => u.TelegramId == telegramId);

    public static DemoSeedUserSpec? FindByTelegramId(long telegramId) =>
        Users.FirstOrDefault(u => u.TelegramId == telegramId);

    private static IReadOnlyList<DemoSeedUserSpec> Build()
    {
        // Индексы интересов — позиция в Blizka.Data.Seed.InterestSeed.All (0-based, порядок фиксирован там же:
        // 0-7 спорт, 8-15 творчество, 16-23 развлечения, 24-31 еда и напитки, 32-39 рост и путешествия).
        var rows = new (string First, string Last, Gender Gender, DateOnly BirthDate, DatingGoal Goal, string Bio, int Photos, int[] Interests)[]
        {
            ("Алина", "Демо", Gender.Female, new DateOnly(2001, 5, 14), DatingGoal.LongTermRelationship,
                "Люблю утренние пробежки и хороший кофе. Ищу серьёзные отношения.", 3, [0, 25, 32]),
            ("Богдан", "Демо", Gender.Male, new DateOnly(1998, 11, 2), DatingGoal.Casual,
                "Программист, по выходным играю на гитаре. Открыт новым знакомствам.", 2, [8, 17, 33]),
            ("Вероника", "Демо", Gender.Female, new DateOnly(1995, 3, 21), DatingGoal.Friendship,
                "Путешествую при первой возможности, обожаю настольные игры.", 3, [32, 16, 4]),
            ("Глеб", "Демо", Gender.Male, new DateOnly(2003, 9, 10), DatingGoal.NotSureYet,
                "Учусь на дизайнера, рисую по вечерам.", 2, [9, 14, 35]),
            ("Дарья", "Демо", Gender.Female, new DateOnly(1999, 7, 30), DatingGoal.FamilyAndKids,
                "Врач, люблю готовить и печь по выходным.", 3, [24, 30, 2]),
            ("Егор", "Демо", Gender.Male, new DateOnly(1996, 12, 5), DatingGoal.HobbyCompany,
                "Хожу в походы, играю в волейбол.", 2, [5, 4, 6]),
            ("Жанна", "Демо", Gender.Female, new DateOnly(2004, 2, 18), DatingGoal.Chatting,
                "Студентка, обожаю сериалы и аниме.", 2, [23, 22, 17]),
            ("Иван", "Демо", Gender.Male, new DateOnly(1992, 6, 27), DatingGoal.LongTermRelationship,
                "Инженер, увлекаюсь фотографией и вином.", 3, [10, 26, 1]),
            ("Ксения", "Демо", Gender.Female, new DateOnly(1997, 10, 9), DatingGoal.Casual,
                "Маркетолог, люблю стендап и вечеринки.", 2, [19, 20, 3]),
            ("Максим", "Демо", Gender.Male, new DateOnly(2000, 4, 16), DatingGoal.Friendship,
                "Занимаюсь боевыми искусствами, слушаю музыку.", 3, [7, 8, 31]),
        };

        var result = new List<DemoSeedUserSpec>(rows.Length);
        for (var i = 0; i < rows.Length; i++)
        {
            var index = i + 1;
            var row = rows[i];
            result.Add(new DemoSeedUserSpec(
                index,
                TelegramIdRangeStart + i,
                $"demo_user_{index}",
                row.First,
                row.Last,
                row.Gender,
                row.BirthDate,
                row.Goal,
                row.Bio,
                row.Photos,
                row.Interests));
        }

        return result;
    }
}
