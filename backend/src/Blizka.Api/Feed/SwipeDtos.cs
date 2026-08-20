using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Swipes;

namespace Blizka.Api.Feed;

/// <summary>Ответ <c>POST /api/feed/{userId}/like|dislike|superlike</c> (T-5.2).</summary>
/// <param name="Action">Тип свайпа — сериализуется в camelCase (<c>like</c>/<c>dislike</c>/<c>superlike</c>), см. глобальный <c>JsonStringEnumConverter</c> в <c>ApiServiceCollectionExtensions</c>.</param>
/// <param name="IsMatch">Лайк оказался взаимным — создан мэтч.</param>
/// <param name="Match">Данные мэтча (S-16) — только когда <c>isMatch: true</c>.</param>
/// <param name="SparksBalance">Баланс зорок текущего пользователя после операции.</param>
public sealed record SwipeResponse(SwipeType Action, bool IsMatch, MatchDto? Match, int SparksBalance)
{
    public static SwipeResponse From(SwipeResult result) =>
        new(result.Action, result.IsMatch, result.Match is null ? null : MatchDto.From(result.Match), result.SparksBalance);
}

/// <summary>Данные нового мэтча — карточка на экране S-16 с тремя лёгкими входами для начала общения.</summary>
public sealed record MatchDto(Guid MatchId, Guid UserId, string Name, IcebreakerDto[] Icebreakers)
{
    public static MatchDto From(MatchResult result) =>
        new(result.MatchId, result.UserId, result.Name, result.Icebreakers.Select(IcebreakerDto.From).ToArray());
}

public sealed record IcebreakerDto(string Type, string Label, string Effort)
{
    public static IcebreakerDto From(IcebreakerResult result) => new(result.Type, result.Label, result.Effort);
}

/// <summary>Ответ <c>POST /api/feed/undo</c> (T-5.3).</summary>
/// <param name="Action">Тип отменённого свайпа (<c>like</c>/<c>dislike</c>/<c>superlike</c>).</param>
/// <param name="UserId">Пользователь, которого касался отменённый свайп — возвращается в пул ленты.</param>
/// <param name="UndosRemaining">Сколько отмен ещё доступно за скользящее окно 24 часа (максимум 3).</param>
/// <param name="SparksBalance">Баланс зорок текущего пользователя после операции (не меняется, если отменённый свайп не был суперлайком).</param>
public sealed record UndoSwipeResponse(SwipeType Action, Guid UserId, int UndosRemaining, int SparksBalance)
{
    public static UndoSwipeResponse From(UndoSwipeResult result) =>
        new(result.Type, result.UserId, result.UndosRemaining, result.SparksBalance);
}
