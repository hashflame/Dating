using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Users;

namespace Blizka.App.UseCases.Likes;

/// <summary>Общая проекция <see cref="LikeEntry"/> → <see cref="LikeUserResult"/>, разделяемая между тремя use case'ами T-6.1.</summary>
internal static class LikeResultMapper
{
    /// <summary>
    /// Батч-версия <see cref="ToUserResult"/> — один запрос приватности на весь список (по образцу
    /// <c>GetFeedQueryHandler</c>), а не N+1.
    /// </summary>
    public static List<LikeUserResult> ToUserResults(
        IReadOnlyList<LikeEntry> entries, IReadOnlyDictionary<Guid, PrivacySettings> privacyByUserId) =>
        entries
            .Select(entry => ToUserResult(entry, privacyByUserId.TryGetValue(entry.User.Id, out var p) && p.HideAge))
            .ToList();

    /// <summary>
    /// <paramref name="hideAge"/> — настройка приватности <c>entry.User</c> самого (T-16.1): раньше читалась
    /// только в ленте, из-за чего <c>hideAge</c> обходился через списки симпатий (баг из тикета ClickUp).
    /// </summary>
    private static LikeUserResult ToUserResult(LikeEntry entry, bool hideAge)
    {
        var mainPhoto = entry.User.Photos.FirstOrDefault(p => p.IsMain)
            ?? entry.User.Photos.OrderBy(p => p.SortOrder).FirstOrDefault();

        return new LikeUserResult(
            entry.User.Id, entry.User.Name, hideAge ? null : AgeCalculator.Calculate(entry.User.BirthDate), mainPhoto?.Url,
            IsMatched: entry.MatchId is not null, entry.MatchId);
    }
}
