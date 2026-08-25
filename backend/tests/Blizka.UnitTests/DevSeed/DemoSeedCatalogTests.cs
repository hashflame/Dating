using Blizka.App.DevSeed;

namespace Blizka.UnitTests.DevSeed;

public sealed class DemoSeedCatalogTests
{
    [Fact(DisplayName = "КОГДА читаем каталог демо-пользователей ТОГДА их ровно 10 с уникальными TelegramId и Username")]
    public void Catalog_has_exactly_ten_users_with_unique_telegram_ids_and_usernames()
    {
        Assert.Equal(10, DemoSeedCatalog.Users.Count);
        Assert.Equal(10, DemoSeedCatalog.Users.Select(u => u.TelegramId).Distinct().Count());
        Assert.Equal(10, DemoSeedCatalog.Users.Select(u => u.Username).Distinct().Count());
    }

    [Fact(DisplayName = "КОГДА читаем TelegramId демо-пользователей ТОГДА все они внутри зарезервированного блока")]
    public void All_telegram_ids_are_within_the_reserved_range()
    {
        foreach (var user in DemoSeedCatalog.Users)
        {
            Assert.InRange(user.TelegramId, DemoSeedCatalog.TelegramIdRangeStart, DemoSeedCatalog.TelegramIdRangeStart + 9);
        }
    }

    [Fact(DisplayName = "КОГДА TelegramId принадлежит демо-пользователю ТОГДА IsDemoTelegramId/FindByTelegramId его находят")]
    public void IsDemoTelegramId_and_FindByTelegramId_recognize_a_demo_user()
    {
        var demoUser = DemoSeedCatalog.Users[0];

        Assert.True(DemoSeedCatalog.IsDemoTelegramId(demoUser.TelegramId));
        Assert.Same(demoUser, DemoSeedCatalog.FindByTelegramId(demoUser.TelegramId));
    }

    [Fact(DisplayName = "КОГДА TelegramId не из 10 демо ТОГДА IsDemoTelegramId/FindByTelegramId возвращают отрицательный результат")]
    public void IsDemoTelegramId_and_FindByTelegramId_reject_a_non_demo_telegram_id()
    {
        const long realLookingTelegramId = 123456789;

        Assert.False(DemoSeedCatalog.IsDemoTelegramId(realLookingTelegramId));
        Assert.Null(DemoSeedCatalog.FindByTelegramId(realLookingTelegramId));
    }
}
