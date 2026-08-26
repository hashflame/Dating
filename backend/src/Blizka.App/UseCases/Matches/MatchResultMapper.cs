using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Cities;

namespace Blizka.App.UseCases.Matches;

/// <summary>Общие проекции <see cref="Match"/> для use case'ов T-7.1 и T-7.2.</summary>
internal static class MatchResultMapper
{
    /// <summary>Разбирает канонизированную пару <see cref="Match.User1"/>/<see cref="Match.User2"/> на «текущий пользователь» и «второй участник» относительно <paramref name="userId"/>.</summary>
    public static (User Me, User Other) ResolveUsers(Match match, Guid userId) =>
        match.User1Id == userId ? (match.User1!, match.User2!) : (match.User2!, match.User1!);

    public static MatchUserResult ToUserResult(User user)
    {
        var mainPhoto = user.Photos.FirstOrDefault(p => p.IsMain)
            ?? user.Photos.OrderBy(p => p.SortOrder).FirstOrDefault();

        return new MatchUserResult(user.Id, user.Name, CalculateAge(user.BirthDate), mainPhoto?.Url);
    }

    /// <summary>
    /// Второй участник для хаба мэтча (T-7.2) — в отличие от <see cref="ToUserResult"/> добавляет город, lastActive
    /// и telegramUsername (последний — только если <paramref name="contactUnlocked"/>, spec.md 8.2). <paramref name="showLastActive"/> —
    /// настройка приватности второго участника (T-16.1): <c>false</c> скрывает <c>lastActive</c>, как и в ленте.
    /// </summary>
    public static MatchHubUserResult ToHubUserResult(User user, bool contactUnlocked, bool showLastActive, CityLocale locale)
    {
        var mainPhoto = user.Photos.FirstOrDefault(p => p.IsMain)
            ?? user.Photos.OrderBy(p => p.SortOrder).FirstOrDefault();
        var cityName = user.City is null ? string.Empty : CityNameResolver.Resolve(user.City, locale);

        return new MatchHubUserResult(
            user.Id,
            user.Name,
            CalculateAge(user.BirthDate),
            cityName,
            showLastActive ? user.LastActiveAt : null,
            contactUnlocked ? user.TelegramUsername : null,
            mainPhoto?.Url);
    }

    private static int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
        var age = today.Year - birthDate.Year;
        if (today < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }
}
