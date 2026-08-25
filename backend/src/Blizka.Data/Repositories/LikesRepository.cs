using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class LikesRepository(BlizkaDbContext dbContext) : ILikesRepository
{
    public Task<int> CountIncomingAsync(Guid userId, CancellationToken cancellationToken) =>
        IncomingQuery(userId).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<LikeEntry>> GetIncomingPreviewAsync(Guid userId, int limit, CancellationToken cancellationToken)
    {
        var swipes = await IncomingQuery(userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .Include(s => s.FromUser!).ThenInclude(u => u!.Photos)
            .ToListAsync(cancellationToken);

        return swipes.Select(s => new LikeEntry(s.FromUser!, s.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<LikeEntry>> GetIncomingAsync(Guid userId, CancellationToken cancellationToken)
    {
        var swipes = await IncomingQuery(userId)
            .OrderByDescending(s => s.CreatedAt)
            .Include(s => s.FromUser!).ThenInclude(u => u!.Photos)
            .ToListAsync(cancellationToken);

        return swipes.Select(s => new LikeEntry(s.FromUser!, s.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<LikeEntry>> GetOutgoingAsync(Guid userId, CancellationToken cancellationToken)
    {
        var swipes = await OutgoingQuery(userId)
            .OrderByDescending(s => s.CreatedAt)
            .Include(s => s.ToUser!).ThenInclude(u => u!.Photos)
            .ToListAsync(cancellationToken);

        return swipes.Select(s => new LikeEntry(s.ToUser!, s.CreatedAt)).ToList();
    }

    // Активный (не отменённый) лайк/суперлайк в мой адрес, чья пара ещё не образовала Match (в любом статусе) —
    // смэтченные показываются в мэтчах (T-7.1), не здесь. FromUser.Status != Deleted — иначе удалённый аккаунт
    // (User.Status = Deleted, soft delete по T-16.2) оставался в списке навсегда: пользователь платит зорки за
    // разблокировку списка, где часть — уже удалённые профили (найдено вручную, тикет ClickUp).
    private IQueryable<Swipe> IncomingQuery(Guid userId) =>
        dbContext.Swipes
            .AsNoTracking()
            .Where(s => s.ToUserId == userId && s.UndoneAt == null &&
                (s.Type == SwipeType.Like || s.Type == SwipeType.Superlike) &&
                s.FromUser!.Status != UserStatus.Deleted)
            .Where(s => !dbContext.Matches.Any(m =>
                (m.User1Id == userId && m.User2Id == s.FromUserId) ||
                (m.User1Id == s.FromUserId && m.User2Id == userId)));

    private IQueryable<Swipe> OutgoingQuery(Guid userId) =>
        dbContext.Swipes
            .AsNoTracking()
            .Where(s => s.FromUserId == userId && s.UndoneAt == null &&
                (s.Type == SwipeType.Like || s.Type == SwipeType.Superlike) &&
                s.ToUser!.Status != UserStatus.Deleted)
            .Where(s => !dbContext.Matches.Any(m =>
                (m.User1Id == userId && m.User2Id == s.ToUserId) ||
                (m.User1Id == s.ToUserId && m.User2Id == userId)));
}
