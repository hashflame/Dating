using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Swipes;

/// <summary>
/// Результат отмены последнего свайпа (T-5.3). <paramref name="UserId"/> — пользователь, которого отменённый
/// свайп касался (он возвращается в пул ленты); <paramref name="Type"/> — тип отменённого свайпа.
/// </summary>
public sealed record UndoSwipeResult(Guid UserId, SwipeType Type, int UndosRemaining, int SparksBalance);
