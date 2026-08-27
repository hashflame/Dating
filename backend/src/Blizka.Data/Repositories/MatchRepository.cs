using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Matches;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class MatchRepository(BlizkaDbContext dbContext) : IMatchRepository
{
    public async Task AddAsync(Match match, CancellationToken cancellationToken) =>
        await dbContext.Matches.AddAsync(match, cancellationToken);

    public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken)
    {
        var (user1Id, user2Id) = userId1.CompareTo(userId2) < 0 ? (userId1, userId2) : (userId2, userId1);

        return dbContext.Matches.SingleOrDefaultAsync(
            m => m.User1Id == user1Id && m.User2Id == user2Id, cancellationToken);
    }

    public void Remove(Match match) => dbContext.Matches.Remove(match);

    public async Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
        await WithUsers(ForUser(userId))
            .Where(m => m.Status == MatchStatus.Active && m.ContactUnlockedAt == null)
            .OrderByDescending(m => m.MatchedAt)
            .ToListAsync(cancellationToken);

    // Без WithUsers — в отличие от GetNewAsync, счётчику не нужны фото/интересы/город обоих участников
    // (T-10.2, бейдж непрочитанного); тот же фильтр по секции «new», но без Include/AsSplitQuery.
    public Task<int> CountNewAsync(Guid userId, CancellationToken cancellationToken) =>
        ForUser(userId)
            .Where(m => m.Status == MatchStatus.Active && m.ContactUnlockedAt == null)
            .CountAsync(cancellationToken);

    public Task<int> CountNewSinceAsync(Guid userId, DateTimeOffset? since, CancellationToken cancellationToken) =>
        ForUser(userId)
            .Where(m => m.Status == MatchStatus.Active && m.ContactUnlockedAt == null)
            .Where(m => since == null || m.MatchedAt > since)
            .CountAsync(cancellationToken);

    // MessageSentCheckAt намеренно не фильтруется — иначе мэтч, по которому уже прошла проверка отправки
    // сообщения, пропадал бы из всех трёх секций разом (баг T-7.1: не попадает ни в new, ни в waiting, ни в
    // archived, хотя GET /api/matches/{matchId} по-прежнему отдаёт его). MessageSentCheckAt используется только
    // в MatchArchivalPolicy.IsStale, чтобы такой мэтч не протухал автоматически.
    public async Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
        await WithUsers(ForUser(userId))
            .Where(m => m.Status == MatchStatus.Active && m.ContactUnlockedAt != null)
            .OrderByDescending(m => m.ContactUnlockedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
        await WithUsers(ForUser(userId))
            .Where(m => m.Status == MatchStatus.Archived)
            .OrderByDescending(m => m.ArchivedAt ?? m.MatchedAt)
            .ToListAsync(cancellationToken);

    public async Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
        await WithUsers(ForUser(userId))
            .SingleOrDefaultAsync(m => m.Id == matchId, cancellationToken);

    // Без WithUsers, в отличие от GetByIdForUserAsync выше — T-11.1 (вопрос дня и его архив) нужны только
    // Id/User1Id/User2Id и базовые поля участников (например, Locale), не фото/интересы/предпочтения/город.
    public async Task<Match?> GetByIdForUserBasicAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Matches
            .AsNoTracking()
            .Include(m => m.User1)
            .Include(m => m.User2)
            .Where(m => m.User1Id == userId || m.User2Id == userId)
            .SingleOrDefaultAsync(m => m.Id == matchId, cancellationToken);

    // Намеренно без AsNoTracking (в отличие от GetByIdForUserAsync выше) — write-путь T-7.3 должен видеть
    // мутации Match/User.SparksBalance при SaveChangesAsync.
    public async Task<Match?> GetByIdForUserTrackedAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Matches
            .Include(m => m.User1)
            .Include(m => m.User2)
            .Where(m => m.User1Id == userId || m.User2Id == userId)
            .SingleOrDefaultAsync(m => m.Id == matchId, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // User — гонка того же пользователя (например, двойной клик по «Открыть контакт»). Match — гонка
            // между двумя разными участниками мэтча, почти одновременно открывающими один и тот же контакт:
            // оба списывают зорки каждый со своего баланса (разные строки User, конфликта нет), но пишут в
            // один и тот же Match — без xmin-токена на Match вторая транзакция тихо перезаписала бы первую
            // и оба заплатили бы за одно и то же открытие контакта (T-7.3).
            var conflictingUser = dbContext.ChangeTracker.Entries<User>()
                .Select(entry => entry.Entity)
                .FirstOrDefault(user => dbContext.Entry(user).State == EntityState.Modified);
            if (conflictingUser is not null)
            {
                throw new ConcurrentUserUpdateException(conflictingUser.Id, ex);
            }

            var conflictingMatch = dbContext.ChangeTracker.Entries<Match>()
                .Select(entry => entry.Entity)
                .FirstOrDefault(match => dbContext.Entry(match).State == EntityState.Modified);

            throw new ConcurrentUserUpdateException(conflictingMatch?.ContactUnlockedByUserId ?? Guid.Empty, ex);
        }
    }

    public Task<int> ArchiveStaleMatchesAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var threshold = now - MatchArchivalPolicy.StaleAfter;

        // MatchArchivalPolicy.IsStale выражает то же условие для уже загруженной в память сущности
        // (GetMatchesQueryHandler) — здесь оно продублировано как LINQ-предикат, потому что вызов
        // произвольного C#-метода внутри Where для ExecuteUpdateAsync EF Core в SQL не транслирует.
        return dbContext.Matches
            .Where(m => m.Status == MatchStatus.Active
                && ((m.ContactUnlockedAt == null && m.MatchedAt < threshold)
                    || (m.ContactUnlockedAt != null && m.MessageSentCheckAt == null && m.ContactUnlockedAt < threshold)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.Status, MatchStatus.Archived)
                .SetProperty(m => m.ArchivedAt, now)
                .SetProperty(m => m.ArchivedReason, MatchArchivalPolicy.AutoArchivedReason), cancellationToken);
    }

    // Партнёр не должен быть Deleted — как и в LikesRepository (IncomingQuery/OutgoingQuery), удалённый
    // пользователь не должен всплывать в списках мэтчей другого участника (T-7.1).
    public async Task RemoveAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var matches = await dbContext.Matches
            .Where(m => m.User1Id == userId || m.User2Id == userId)
            .ToListAsync(cancellationToken);

        dbContext.Matches.RemoveRange(matches);
    }

    private IQueryable<Match> ForUser(Guid userId) =>
        dbContext.Matches.AsNoTracking().Where(m =>
            (m.User1Id == userId && m.User2!.Status != UserStatus.Deleted)
            || (m.User2Id == userId && m.User1!.Status != UserStatus.Deleted));

    // Обе стороны мэтча грузятся целиком (фото, интересы+Interest, город) — вторая сторона идёт в проекцию
    // MatchUserResult, а своя сторона (кто из User1/User2 совпал с userId) нужна для FeedCompatibilityScorer
    // при подсчёте бейджа fire в секции new. AsSplitQuery — по тому же соображению, что и в FeedRepository:
    // две коллекции (Photos, UserInterests) на двух связанных сущностях иначе дали бы декартово произведение.
    private static IQueryable<Match> WithUsers(IQueryable<Match> query) =>
        query
            .Include(m => m.User1!).ThenInclude(u => u!.Photos)
            .Include(m => m.User1!).ThenInclude(u => u!.UserInterests).ThenInclude(ui => ui.Interest)
            .Include(m => m.User1!).ThenInclude(u => u!.UserDatePreferences).ThenInclude(p => p.DatePreference)
            .Include(m => m.User1!).ThenInclude(u => u!.City)
            .Include(m => m.User2!).ThenInclude(u => u!.Photos)
            .Include(m => m.User2!).ThenInclude(u => u!.UserInterests).ThenInclude(ui => ui.Interest)
            .Include(m => m.User2!).ThenInclude(u => u!.UserDatePreferences).ThenInclude(p => p.DatePreference)
            .Include(m => m.User2!).ThenInclude(u => u!.City)
            .AsSplitQuery();
}
