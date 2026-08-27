using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Cities;
using Blizka.App.UseCases.Users;

namespace Blizka.App.UseCases.Matches;

/// <summary>Общие проекции <see cref="Match"/> для use case'ов T-7.1 и T-7.2.</summary>
internal static class MatchResultMapper
{
    /// <summary>Разбирает канонизированную пару <see cref="Match.User1"/>/<see cref="Match.User2"/> на «текущий пользователь» и «второй участник» относительно <paramref name="userId"/>.</summary>
    public static (User Me, User Other) ResolveUsers(Match match, Guid userId) =>
        match.User1Id == userId ? (match.User1!, match.User2!) : (match.User2!, match.User1!);

    /// <summary>
    /// <paramref name="hideAge"/> — настройка приватности <paramref name="user"/> самого (T-16.1): раньше
    /// читалась только в ленте (<c>GetFeedQueryHandler</c>), из-за чего <c>hideAge</c> обходился через списки
    /// мэтчей (баг из тикета ClickUp).
    /// </summary>
    public static MatchUserResult ToUserResult(User user, bool hideAge)
    {
        var mainPhoto = user.Photos.FirstOrDefault(p => p.IsMain)
            ?? user.Photos.OrderBy(p => p.SortOrder).FirstOrDefault();

        return new MatchUserResult(user.Id, user.Name, hideAge ? null : AgeCalculator.Calculate(user.BirthDate), mainPhoto?.Url);
    }

    /// <summary>
    /// Второй участник для хаба мэтча (T-7.2) — в отличие от <see cref="ToUserResult"/> добавляет город, lastActive
    /// и telegramUsername (последний — только если <paramref name="contactUnlocked"/>, spec.md 8.2). <paramref name="showLastActive"/>
    /// и <paramref name="hideAge"/> — настройки приватности второго участника (T-16.1).
    /// </summary>
    public static MatchHubUserResult ToHubUserResult(
        User user, bool contactUnlocked, bool showLastActive, bool hideAge, CityLocale locale)
    {
        var mainPhoto = user.Photos.FirstOrDefault(p => p.IsMain)
            ?? user.Photos.OrderBy(p => p.SortOrder).FirstOrDefault();
        var cityName = user.City is null ? string.Empty : CityNameResolver.Resolve(user.City, locale);

        return new MatchHubUserResult(
            user.Id,
            user.Name,
            hideAge ? null : AgeCalculator.Calculate(user.BirthDate),
            cityName,
            showLastActive ? user.LastActiveAt : null,
            contactUnlocked ? user.TelegramUsername : null,
            mainPhoto?.Url);
    }
}
