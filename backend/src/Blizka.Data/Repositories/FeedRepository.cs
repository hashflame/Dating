using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Feed;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class FeedRepository(BlizkaDbContext dbContext) : IFeedRepository
{
    public Task<User?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users
            .Include(u => u.City)
            .Include(u => u.UserInterests)
            .Include(u => u.UserDatePreferences)
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public async Task<IReadOnlyList<User>> GetCandidatesAsync(
        Guid currentUserId, FeedCandidateFilter filter, int poolSize, CancellationToken cancellationToken)
    {
        var swipedUserIds = dbContext.Swipes
            .Where(s => s.FromUserId == currentUserId && s.UndoneAt == null)
            .Select(s => s.ToUserId);

        // T-16.2 — блокировка скрывает пару друг от друга в обе стороны: не только тех, кого заблокировал
        // текущий пользователь, но и тех, кто заблокировал его самого.
        var blockedUserIds = dbContext.UserBlocks
            .Where(b => b.BlockerUserId == currentUserId || b.BlockedUserId == currentUserId)
            .Select(b => b.BlockerUserId == currentUserId ? b.BlockedUserId : b.BlockerUserId);

        // Невидимый режим (T-16.1 PrivacySettings.InvisibleMode) сохранялся и отдавался в настройках, но никак
        // не влиял на выборку кандидатов — анкета с включённым режимом продолжала всплывать в чужих лентах
        // (найдено вручную, тикет ClickUp). Строки PrivacySettings нет, пока пользователь ни разу не сохранял
        // настройки (см. PrivacySettingsDefaults) — тогда InvisibleMode по умолчанию false, скрывать некого.
        var invisibleUserIds = dbContext.PrivacySettings
            .Where(p => p.InvisibleMode)
            .Select(p => p.UserId);

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);

        var query = dbContext.Users
            .Where(u => u.Id != currentUserId)
            .Where(u => u.Status == UserStatus.Active)
            .Where(u => !swipedUserIds.Contains(u.Id))
            .Where(u => !blockedUserIds.Contains(u.Id))
            .Where(u => !invisibleUserIds.Contains(u.Id))
            // T-5.4 заменил строгое совпадение города (T-5.1) на радиус: своя геолокация — приоритет, город —
            // запасной источник координат (тот же fallback, что и в FeedCompatibilityScorer, но здесь на SQL,
            // т.к. City.Coordinates не nullable — COALESCE не бывает NULL, если у кандидата вообще есть город).
            .Where(u => (u.Coordinates ?? u.City!.Coordinates).Distance(filter.OriginCoordinates) <= filter.MaxDistanceMeters);

        if (filter.PreferredGender is { } preferredGender)
        {
            query = query.Where(u => u.Gender == preferredGender);
        }

        if (filter.AgeMin is { } ageMin)
        {
            // Возраст >= ageMin  <=>  BirthDate <= "дата, на которую человеку исполняется ровно ageMin сегодня".
            var maxBirthDate = today.AddYears(-ageMin);
            query = query.Where(u => u.BirthDate <= maxBirthDate);
        }

        if (filter.AgeMax is { } ageMax)
        {
            // Возраст <= ageMax  <=>  BirthDate позже даты, на которую исполнилось бы ageMax + 1.
            var minBirthDateExclusive = today.AddYears(-(ageMax + 1));
            query = query.Where(u => u.BirthDate > minBirthDateExclusive);
        }

        if (filter.RequireFilledProfile)
        {
            query = query.Where(u => u.ProfileCompleteness >= UserFilterDefaults.RequireFilledProfileMinCompleteness);
        }

        if (filter.ActiveWithinDays is { } activeWithinDays)
        {
            var activeSince = DateTimeOffset.UtcNow.AddDays(-activeWithinDays);
            query = query.Where(u => u.LastActiveAt != null && u.LastActiveAt >= activeSince);
        }

        if (filter.RequirePhoto)
        {
            query = query.Where(u => u.Photos.Any());
        }

        if (filter.VerifiedOnly)
        {
            query = query.Where(u => u.IsVerified);
        }

        if (filter.NonSmoker)
        {
            query = query.Where(u => u.Smoking == SmokingHabit.No);
        }

        if (filter.NonDrinker)
        {
            query = query.Where(u => u.Drinking == DrinkingHabit.No);
        }

        if (filter.NoChildren)
        {
            // HasChildren не заполняется нигде (T-5.4, нет источника в онбординге/профиле) — отсеиваем только
            // явное true, null (не указано) и false пропускаем, иначе фильтр скрыл бы вообще всех кандидатов.
            query = query.Where(u => u.HasChildren != true);
        }

        // Postgres по умолчанию сортирует NULL первыми при DESC — без .HasValue-ключа пользователи без
        // LastActiveAt оказались бы в начале пула вместо конца, вытесняя недавно активных за пределы poolSize.
        var orderedQuery = query
            .OrderByDescending(u => u.LastActiveAt.HasValue)
            .ThenByDescending(u => u.LastActiveAt);

        IReadOnlyList<Guid> orderedCandidateIds;
        if (filter.DatingGoals is { Count: > 0 } datingGoals)
        {
            // u.DatingGoals — DatingGoal[], хранится через поэлементный value converter (UserConfiguration) —
            // такой конвертер EF Core/Npgsql не умеет протолкнуть внутрь предиката Where/Any (падает с
            // "could not be translated" — проверено вручную на реальном Postgres). Поэтому пересечение с
            // фильтром считается на клиенте: сначала тянем упорядоченные id+DatingGoals без Take (сама выборка
            // такого масштаба, что это не проблема для MVP), затем фильтруем и обрезаем до poolSize в памяти.
            var idsWithGoals = await orderedQuery
                .Select(u => new { u.Id, u.DatingGoals })
                .ToListAsync(cancellationToken);

            orderedCandidateIds = idsWithGoals
                .Where(u => u.DatingGoals.Any(datingGoals.Contains))
                .Select(u => u.Id)
                .Take(poolSize)
                .ToList();
        }
        else
        {
            orderedCandidateIds = await orderedQuery
                .Select(u => u.Id)
                .Take(poolSize)
                .ToListAsync(cancellationToken);
        }

        var candidates = await dbContext.Users
            .Where(u => orderedCandidateIds.Contains(u.Id))
            .Include(u => u.Photos)
            .Include(u => u.UserInterests)
                .ThenInclude(ui => ui.Interest)
            .Include(u => u.UserDatePreferences)
            .Include(u => u.City)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var candidatesById = candidates.ToDictionary(u => u.Id);
        return orderedCandidateIds.Select(id => candidatesById[id]).ToList();
    }
}
