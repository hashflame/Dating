using Blizka.App.Domain.Enums;
using Blizka.App.Notifications;

namespace Blizka.UnitTests.Notifications;

public sealed class NotificationMessageCatalogTests
{
    [Theory(DisplayName = "КОГДА Build для Match с плейсхолдером ТОГДА имя мэтча подставлено в текст локали")]
    [InlineData(CityLocale.Ru, "У вас новый мэтч с Анна!")]
    [InlineData(CityLocale.Be, "У вас новы мэтч з Анна!")]
    [InlineData(CityLocale.En, "You have a new match with Анна!")]
    public void Build_substitutes_the_placeholder_for_match(CityLocale locale, string expected)
    {
        var text = NotificationMessageCatalog.Build(NotificationType.Match, locale, "Анна");

        Assert.Equal(expected, text);
    }

    [Fact(DisplayName = "КОГДА Build для NewProfiles без плейсхолдера ТОГДА возвращается текст шаблона как есть")]
    public void Build_returns_the_template_verbatim_when_there_is_no_placeholder()
    {
        var text = NotificationMessageCatalog.Build(NotificationType.NewProfiles, CityLocale.Ru, placeholder: null);

        Assert.Equal("Появились новые анкеты", text);
    }
}
