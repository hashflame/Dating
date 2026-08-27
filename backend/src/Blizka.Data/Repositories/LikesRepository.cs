using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class LikesRepository(BlizkaDbContext dbContext) : ILikesRepository
{
    public Task<int> CountIncomingAsync(Guid userId, CancellationToken cancellationToken) =>
        IncomingQuery(userId).CountAsync(cancellationToken);

    public Task<int> CountIncomingSinceAsync(Guid userId, DateTimeOffset? since, CancellationToken cancellationToken) =>
        IncomingQuery(userId)
            .Where(s => since == null || s.CreatedAt > since)
            .CountAsync(cancellationToken);

    public async Task<IReadOnlyList<LikeEntry>> GetIncomingPreviewAsync(Guid userId, int limit, CancellationToken cancellationToken)
    {
        var swipes = await IncomingQuery(userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .Include(s => s.FromUser!).ThenInclude(u => u!.Photos)
            .ToListAsync(cancellationToken);

        return swipes.Select(s => new LikeEntry(s.FromUser!, s.CreatedAt, MatchId: null)).ToList();
    }

    // Без исключения смэтченных (в отличие от IncomingQuery/CountIncomingAsync) — иначе человек, ответивший
    // взаимностью на входящую симпатию, молча исчезал бы из уже разблокированного списка (баг из тикета
    // ClickUp: список, за который заплатили зорками, укорачивался без предупреждения). MatchId вместо этого
    // подмешивается в проекцию — фронт сам решает, показывать смэтченных или нет (тумблер).
    public async Task<IReadOnlyList<LikeEntry>> GetIncomingAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await IncomingBaseQuery(userId)
            .OrderByDescending(s => s.CreatedAt)
            .Include(s => s.FromUser!).ThenInclude(u => u!.Photos)
            .Select(s => new
            {
                Swipe = s,
                MatchId = dbContext.Matches
                    .Where(m => (m.User1Id == userId && m.User2Id == s.FromUserId) ||
                        (m.User1Id == s.FromUserId && m.User2Id == userId))
                    .Select(m => (Guid?)m.Id)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new LikeEntry(r.Swipe.FromUser!, r.Swipe.CreatedAt, r.MatchId)).ToList();
    }

    // См. комментарий у GetIncomingAsync — та же логика для исходящих симпатий.
    public async Task<IReadOnlyList<LikeEntry>> GetOutgoingAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await OutgoingBaseQuery(userId)
            .OrderByDescending(s => s.CreatedAt)
            .Include(s => s.ToUser!).ThenInclude(u => u!.Photos)
            .Select(s => new
            {
                Swipe = s,
                MatchId = dbContext.Matches
                    .Where(m => (m.User1Id == userId && m.User2Id == s.ToUserId) ||
                        (m.User1Id == s.ToUserId && m.User2Id == userId))
                    .Select(m => (Guid?)m.Id)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new LikeEntry(r.Swipe.ToUser!, r.Swipe.CreatedAt, r.MatchId)).ToList();
    }

    // FromUser.Status != Deleted — иначе удалённый аккаунт (User.Status = Deleted, soft delete по T-16.2)
    // оставался в списке навсегда: пользователь платит зорки за разблокировку списка, где часть — уже удалённые
    // профили (найдено вручную, тикет ClickUp). !blockedUserIds.Contains — блокировка двусторонняя (T-16.2, тот
    // же принцип, что и в FeedRepository.GetCandidatesAsync): заблокированный не должен появляться ни во
    // входящих, ни в исходящих симпатиях (найдено вручную, тикет ClickUp).
    private IQueryable<Swipe> IncomingBaseQuery(Guid userId) =>
        dbContext.Swipes
            .AsNoTracking()
            .Where(s => s.ToUserId == userId && s.UndoneAt == null &&
                (s.Type == SwipeType.Like || s.Type == SwipeType.Superlike) &&
                s.FromUser!.Status != UserStatus.Deleted &&
                !BlockedUserIds(userId).Contains(s.FromUserId));

    private IQueryable<Swipe> OutgoingBaseQuery(Guid userId) =>
        dbContext.Swipes
            .AsNoTracking()
            .Where(s => s.FromUserId == userId && s.UndoneAt == null &&
                (s.Type == SwipeType.Like || s.Type == SwipeType.Superlike) &&
                s.ToUser!.Status != UserStatus.Deleted &&
                !BlockedUserIds(userId).Contains(s.ToUserId));

    private IQueryable<Guid> BlockedUserIds(Guid userId) =>
        dbContext.UserBlocks
            .Where(b => b.BlockerUserId == userId || b.BlockedUserId == userId)
            .Select(b => b.BlockerUserId == userId ? b.BlockedUserId : b.BlockerUserId);

    // Активный (не отменённый) лайк/суперлайк, чья пара ещё не образовала Match (в любом статусе) — используется
    // только для счётчика (CountIncomingAsync/CountIncomingSinceAsync) и превью до разблокировки: там смэтченные
    // по-прежнему исключаются, чтобы число «новых» не менялось задним числом при разблокированном списке.
    private IQueryable<Swipe> IncomingQuery(Guid userId) =>
        IncomingBaseQuery(userId)
            .Where(s => !dbContext.Matches.Any(m =>
                (m.User1Id == userId && m.User2Id == s.FromUserId) ||
                (m.User1Id == s.FromUserId && m.User2Id == userId)));
}
