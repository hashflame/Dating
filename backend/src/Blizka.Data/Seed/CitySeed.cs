using Blizka.App.Domain.Entities;
using NetTopologySuite.Geometries;

namespace Blizka.Data.Seed;

/// <summary>
/// Стартовый гео-справочник (T-4.1): все областные/районные центры и крупные города Беларуси плюс
/// крупные города диаспоры (Польша/Литва/Латвия/Россия/Украина) — точки притяжения белорусской
/// эмиграции, по которым пользователь тоже может искать город при онбординге.
/// </summary>
public static class CitySeed
{
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    public static IReadOnlyList<City> All { get; } = Build();

    private static IReadOnlyList<City> Build()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var rows = new List<(string Ru, string Be, string En, string Country, double Lat, double Lon)>
        {
            ("Минск", "Мінск", "Minsk", "BY", 53.9006, 27.5590),
            ("Гомель", "Гомель", "Homyel", "BY", 52.4345, 30.9754),
            ("Могилёв", "Магілёў", "Mahilyow", "BY", 53.9007, 30.3313),
            ("Витебск", "Віцебск", "Vitsyebsk", "BY", 55.1904, 30.2049),
            ("Гродно", "Гродна", "Hrodna", "BY", 53.6884, 23.8258),
            ("Брест", "Брэст", "Brest", "BY", 52.0975, 23.7341),
            ("Бобруйск", "Бабруйск", "Babruysk", "BY", 53.1500, 29.2167),
            ("Барановичи", "Баранавічы", "Baranavichy", "BY", 53.1333, 26.0167),
            ("Борисов", "Барысаў", "Barysaw", "BY", 54.2333, 28.5000),
            ("Пинск", "Пінск", "Pinsk", "BY", 52.1216, 26.0985),
            ("Орша", "Орша", "Orsha", "BY", 54.5081, 30.4172),
            ("Мозырь", "Мазыр", "Mazyr", "BY", 52.0498, 29.2411),
            ("Солигорск", "Салігорск", "Salihorsk", "BY", 52.7975, 27.5442),
            ("Новополоцк", "Наваполацк", "Navapolatsk", "BY", 55.5333, 28.6500),
            ("Лида", "Ліда", "Lida", "BY", 53.8833, 25.3000),
            ("Молодечно", "Маладзечна", "Maladzyechna", "BY", 54.3167, 26.8500),
            ("Полоцк", "Полацк", "Polatsk", "BY", 55.4833, 28.7833),
            ("Жлобин", "Жлобін", "Zhlobin", "BY", 52.8925, 30.0350),
            ("Светлогорск", "Светлагорск", "Svyetlahorsk", "BY", 52.6333, 29.7333),
            ("Речица", "Рэчыца", "Rechytsa", "BY", 52.3667, 30.4000),
            ("Слуцк", "Слуцк", "Slutsk", "BY", 53.0167, 27.5500),
            ("Кобрин", "Кобрын", "Kobryn", "BY", 52.2136, 24.3667),
            ("Слоним", "Слонім", "Slonim", "BY", 53.0925, 25.3197),
            ("Волковыск", "Ваўкавыск", "Vawkavysk", "BY", 53.1633, 24.4611),
            ("Жодино", "Жодзіна", "Zhodzina", "BY", 54.1000, 28.3333),
            ("Новогрудок", "Навагрудак", "Navahrudak", "BY", 53.5989, 25.8258),
            ("Дзержинск", "Дзяржынск", "Dzyarzhynsk", "BY", 53.6833, 27.1333),
            ("Вилейка", "Вілейка", "Vileyka", "BY", 54.4914, 26.9169),

            // Диаспора — крупные города соседних стран с заметной белорусской эмиграцией (T-4.1).
            ("Варшава", "Варшава", "Warsaw", "PL", 52.2297, 21.0122),
            ("Краков", "Кракаў", "Kraków", "PL", 50.0647, 19.9450),
            ("Вроцлав", "Вроцлаў", "Wrocław", "PL", 51.1079, 17.0385),
            ("Гданьск", "Гданьск", "Gdańsk", "PL", 54.3520, 18.6466),
            ("Белосток", "Беласток", "Białystok", "PL", 53.1325, 23.1688),
            ("Вильнюс", "Вільня", "Vilnius", "LT", 54.6872, 25.2797),
            ("Каунас", "Каўнас", "Kaunas", "LT", 54.8985, 23.9036),
            ("Рига", "Рыга", "Riga", "LV", 56.9496, 24.1052),
            ("Москва", "Масква", "Moscow", "RU", 55.7558, 37.6173),
            ("Санкт-Петербург", "Санкт-Пецярбург", "Saint Petersburg", "RU", 59.9343, 30.3351),
            ("Смоленск", "Смаленск", "Smolensk", "RU", 54.7818, 32.0401),
            ("Киев", "Кіеў", "Kyiv", "UA", 50.4501, 30.5234),
            ("Львов", "Львоў", "Lviv", "UA", 49.8397, 24.0297),
        };

        var result = new List<City>(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            var (ru, be, en, country, lat, lon) = rows[i];
            var point = GeometryFactory.CreatePoint(new Coordinate(lon, lat));
            result.Add(new City
            {
                Id = Guid.Parse($"00000000-0000-0000-0a02-{i + 1:000000000000}"),
                NameRu = ru,
                NameBe = be,
                NameEn = en,
                Country = country,
                Coordinates = point,
                IsOpen = true,
                CreatedAt = now,
            });
        }

        return result;
    }
}
