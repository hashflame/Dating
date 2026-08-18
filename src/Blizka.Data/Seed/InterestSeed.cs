using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;

namespace Blizka.Data.Seed;

/// <summary>
/// Стартовый каталог интересов: 5 категорий x 8 интересов. Набор категорий и точные названия —
/// решение по умолчанию (в репозитории нет backend-spec.md, откуда можно было бы их скопировать);
/// продукт может переименовать/реорганизовать их отдельной миграцией, не трогая схему.
/// </summary>
public static class InterestSeed
{
    public static IReadOnlyList<Interest> All { get; } = Build();

    private static IReadOnlyList<Interest> Build()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var rows = new List<(string Ru, string Be, string En, InterestCategory Category)>
        {
            ("Бег", "Бег", "Running", InterestCategory.Sport),
            ("Йога", "Ёга", "Yoga", InterestCategory.Sport),
            ("Тренажёрный зал", "Трэнажорная зала", "Gym", InterestCategory.Sport),
            ("Велоспорт", "Веласпорт", "Cycling", InterestCategory.Sport),
            ("Плавание", "Плаванне", "Swimming", InterestCategory.Sport),
            ("Туризм и походы", "Турызм і паходы", "Hiking", InterestCategory.Sport),
            ("Танцы", "Танцы", "Dancing", InterestCategory.Sport),
            ("Боевые искусства", "Баявыя мастацтвы", "Martial Arts", InterestCategory.Sport),

            ("Музыка", "Музыка", "Music", InterestCategory.Creativity),
            ("Рисование", "Маляванне", "Drawing", InterestCategory.Creativity),
            ("Фотография", "Фатаграфія", "Photography", InterestCategory.Creativity),
            ("Кино", "Кіно", "Movies", InterestCategory.Creativity),
            ("Театр", "Тэатр", "Theatre", InterestCategory.Creativity),
            ("Писательство", "Пісьменства", "Writing", InterestCategory.Creativity),
            ("Дизайн", "Дызайн", "Design", InterestCategory.Creativity),
            ("Рукоделие", "Рукадзелле", "Handicraft", InterestCategory.Creativity),

            ("Настольные игры", "Настольныя гульні", "Board Games", InterestCategory.Entertainment),
            ("Видеоигры", "Відэагульні", "Video Games", InterestCategory.Entertainment),
            ("Караоке", "Караоке", "Karaoke", InterestCategory.Entertainment),
            ("Стендап", "Стэндап", "Stand-up", InterestCategory.Entertainment),
            ("Вечеринки", "Вечарынкі", "Parties", InterestCategory.Entertainment),
            ("Квесты", "Квэсты", "Escape Rooms", InterestCategory.Entertainment),
            ("Аниме", "Аніме", "Anime", InterestCategory.Entertainment),
            ("Сериалы", "Серыялы", "TV Shows", InterestCategory.Entertainment),

            ("Кулинария", "Кулінарыя", "Cooking", InterestCategory.FoodAndDrinks),
            ("Кофе", "Кава", "Coffee", InterestCategory.FoodAndDrinks),
            ("Вино", "Віно", "Wine", InterestCategory.FoodAndDrinks),
            ("Крафтовое пиво", "Крафтавае піва", "Craft Beer", InterestCategory.FoodAndDrinks),
            ("Веганская еда", "Веганская ежа", "Vegan Food", InterestCategory.FoodAndDrinks),
            ("Рестораны", "Рэстараны", "Restaurants", InterestCategory.FoodAndDrinks),
            ("Выпечка", "Выпечка", "Baking", InterestCategory.FoodAndDrinks),
            ("Стритфуд", "Стрытфуд", "Street Food", InterestCategory.FoodAndDrinks),

            ("Путешествия", "Падарожжы", "Travel", InterestCategory.GrowthAndTravel),
            ("Чтение", "Чытанне", "Reading", InterestCategory.GrowthAndTravel),
            ("Языки", "Мовы", "Languages", InterestCategory.GrowthAndTravel),
            ("Медитация", "Медытацыя", "Meditation", InterestCategory.GrowthAndTravel),
            ("Психология", "Псіхалогія", "Psychology", InterestCategory.GrowthAndTravel),
            ("Волонтёрство", "Валанцёрства", "Volunteering", InterestCategory.GrowthAndTravel),
            ("Наука", "Навука", "Science", InterestCategory.GrowthAndTravel),
            ("Стартапы", "Стартапы", "Startups", InterestCategory.GrowthAndTravel),
        };

        var result = new List<Interest>(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            var (ru, be, en, category) = rows[i];
            result.Add(new Interest
            {
                Id = Guid.Parse($"00000000-0000-0000-0a01-{i + 1:000000000000}"),
                Category = category,
                NameRu = ru,
                NameBe = be,
                NameEn = en,
                IsCustom = false,
                CreatedAt = now,
            });
        }

        return result;
    }
}
