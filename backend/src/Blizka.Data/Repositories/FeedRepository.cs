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
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public async Task<IReadOnlyList<User>> GetCandidatesAsync(
        Guid currentUserId, FeedCandidateFilter filter, int poolSize, CancellationToken cancellationToken)
    {
        var swipedUserIds = dbContext.Swipes
            .Where(s => s.FromUserId == currentUserId && s.UndoneAt == null)
            .Select(s => s.ToUserId);

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);

        var query = dbContext.Users
            .Where(u => u.Id != currentUserId)
            .Where(u => u.Status == UserStatus.Active)
            .Where(u => !swipedUserIds.Contains(u.Id))
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

        if (filter.DatingGoals is { Count: > 0 } datingGoals)
        {
            query = query.Where(u => u.DatingGoal != null && datingGoals.Contains(u.DatingGoal!.Value));
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

        return await query
            // Postgres по умолчанию сортирует NULL первыми при DESC — без .HasValue-ключа пользователи без
            // LastActiveAt оказались бы в начале пула вместо конца, вытесняя недавно активных за пределы poolSize.
            .OrderByDescending(u => u.LastActiveAt.HasValue)
            .ThenByDescending(u => u.LastActiveAt)
            .Take(poolSize)
            .Include(u => u.Photos)
            .Include(u => u.UserInterests)
                .ThenInclude(ui => ui.Interest)
            .Include(u => u.City)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
