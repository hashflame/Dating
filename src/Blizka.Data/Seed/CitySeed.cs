using Blizka.App.Domain.Entities;
using NetTopologySuite.Geometries;

namespace Blizka.Data.Seed;

/// <summary>
/// Starter catalog of the largest Belarusian cities with approximate coordinates. This is
/// intentionally a subset, not the full gazetteer — T-4.1 owns seeding every settlement in
/// Belarus plus diaspora cities in Poland/Lithuania/Latvia/Russia/Ukraine.
/// </summary>
public static class CitySeed
{
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    public static IReadOnlyList<City> All { get; } = Build();

    private static IReadOnlyList<City> Build()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var rows = new List<(string Ru, string Be, string En, double Lat, double Lon)>
        {
            ("Минск", "Мінск", "Minsk", 53.9006, 27.5590),
            ("Гомель", "Гомель", "Homyel", 52.4345, 30.9754),
            ("Могилёв", "Магілёў", "Mahilyow", 53.9007, 30.3313),
            ("Витебск", "Віцебск", "Vitsyebsk", 55.1904, 30.2049),
            ("Гродно", "Гродна", "Hrodna", 53.6884, 23.8258),
            ("Брест", "Брэст", "Brest", 52.0975, 23.7341),
            ("Бобруйск", "Бабруйск", "Babruysk", 53.1500, 29.2167),
            ("Барановичи", "Баранавічы", "Baranavichy", 53.1333, 26.0167),
            ("Борисов", "Барысаў", "Barysaw", 54.2333, 28.5000),
            ("Пинск", "Пінск", "Pinsk", 52.1216, 26.0985),
            ("Орша", "Орша", "Orsha", 54.5081, 30.4172),
            ("Мозырь", "Мазыр", "Mazyr", 52.0498, 29.2411),
            ("Солигорск", "Салігорск", "Salihorsk", 52.7975, 27.5442),
            ("Новополоцк", "Наваполацк", "Navapolatsk", 55.5333, 28.6500),
            ("Лида", "Ліда", "Lida", 53.8833, 25.3000),
            ("Молодечно", "Маладзечна", "Maladzyechna", 54.3167, 26.8500),
            ("Полоцк", "Полацк", "Polatsk", 55.4833, 28.7833),
            ("Жлобин", "Жлобін", "Zhlobin", 52.8925, 30.0350),
            ("Светлогорск", "Светлагорск", "Svyetlahorsk", 52.6333, 29.7333),
            ("Речица", "Рэчыца", "Rechytsa", 52.3667, 30.4000),
            ("Слуцк", "Слуцк", "Slutsk", 53.0167, 27.5500),
            ("Кобрин", "Кобрын", "Kobryn", 52.2136, 24.3667),
            ("Слоним", "Слонім", "Slonim", 53.0925, 25.3197),
            ("Волковыск", "Ваўкавыск", "Vawkavysk", 53.1633, 24.4611),
            ("Жодино", "Жодзіна", "Zhodzina", 54.1000, 28.3333),
            ("Новогрудок", "Навагрудак", "Navahrudak", 53.5989, 25.8258),
            ("Дзержинск", "Дзяржынск", "Dzyarzhynsk", 53.6833, 27.1333),
            ("Вилейка", "Вілейка", "Vileyka", 54.4914, 26.9169),
        };

        var result = new List<City>(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            var (ru, be, en, lat, lon) = rows[i];
            var point = GeometryFactory.CreatePoint(new Coordinate(lon, lat));
            result.Add(new City
            {
                Id = Guid.Parse($"00000000-0000-0000-0a02-{i + 1:000000000000}"),
                NameRu = ru,
                NameBe = be,
                NameEn = en,
                Country = "BY",
                Coordinates = point,
                IsOpen = true,
                CreatedAt = now,
            });
        }

        return result;
    }
}
