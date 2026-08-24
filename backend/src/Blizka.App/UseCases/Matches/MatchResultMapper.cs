using Blizka.App.Domain.Entities;

namespace Blizka.App.UseCases.Matches;

/// <summary>Общие проекции <see cref="Match"/> для трёх use case'ов T-7.1.</summary>
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
