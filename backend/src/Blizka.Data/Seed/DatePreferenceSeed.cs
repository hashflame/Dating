using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;

namespace Blizka.Data.Seed;

/// <summary>Фиксированный каталог из 4 пунктов, указанный в T-9.3.</summary>
public static class DatePreferenceSeed
{
    public static IReadOnlyList<DatePreference> All { get; } =
    [
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0a03-000000000001"),
            Code = DatePreferenceCode.ActiveOutdoors,
            NameRu = "Активный отдых на природе",
            NameBe = "Актыўны адпачынак на прыродзе",
            NameEn = "Active outdoors",
        },
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0a03-000000000002"),
            Code = DatePreferenceCode.CalmHangout,
            NameRu = "Спокойные посиделки",
            NameBe = "Спакойныя пасядзелкі",
            NameEn = "Calm hangout",
        },
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0a03-000000000003"),
            Code = DatePreferenceCode.QuizzesBoardGames,
            NameRu = "Квизы и настольные игры",
            NameBe = "Квізы і настольныя гульні",
            NameEn = "Quizzes & board games",
        },
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0a03-000000000004"),
            Code = DatePreferenceCode.SomethingNew,
            NameRu = "Что-то новое",
            NameBe = "Штосьці новае",
            NameEn = "Something new",
        },
    ];
}
