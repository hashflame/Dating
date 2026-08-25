using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Services;
using Blizka.App.DevSeed;
using Blizka.App.UseCases.Matches;
using Blizka.App.UseCases.Photos;
using Blizka.Data.Seed;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.DevSeed;

/// <summary>
/// Пересоздаёт 10 демо-пользователей (спека 003, docs/specs/003-demo-seed-data.md): сносит текущих
/// (если есть) и создаёт заново с тем же детерминированным набором TelegramId/Id (<see cref="DemoSeedCatalog"/>).
/// Фото прогоняются через тот же <see cref="UploadPhotoCommand"/>, что и настоящая загрузка (T-3.1) —
/// thumbnail/medium генерируются и заливаются в реальный MinIO так же, как для обычного пользователя.
/// </summary>
public sealed class DemoSeedService(BlizkaDbContext dbContext, IMediator mediator) : IDemoSeedService
{
    // Стартовый баланс демо-пользователя — без него открытие контакта (✦1, T-7.3), суперлайк (✦5, T-5.2) и
    // разблокировка входящих лайков (✦10, T-6.1) падают с INSUFFICIENT_SPARKS на пустом балансе (SparksBalance
    // по умолчанию 0), хотя эти экраны как раз входят в скоуп ручного тестирования. Ставится напрямую в поле,
    // без SparkTransaction — история зорок сознательно вне скоупа спеки 003.
    private const int StartingSparksBalance = 100;

    // (индекс1, индекс2, контакт открыт?, в архиве?) — индексы соответствуют DemoSeedCatalog.Users[i].Index.
    private static readonly (int A, int B, bool ContactUnlocked, bool Archived)[] MatchPairs =
    [
        (1, 2, true, false),
        (3, 6, false, true),
        (5, 8, false, false),
        (7, 10, false, false),
    ];

    // (от кого, кому) — однонаправленный лайк без ответного мэтча.
    private static readonly (int From, int To)[] OneDirectionalLikes =
    [
        (4, 1),
        (9, 6),
    ];

    public async Task<IReadOnlyList<DemoSeedResultUser>> ReseedAsync(CancellationToken cancellationToken)
    {
        var telegramIds = DemoSeedCatalog.Users.Select(u => u.TelegramId).ToList();

        await DeleteExistingAsync(telegramIds, cancellationToken);

        var minsk = CitySeed.All[0];
        var now = DateTimeOffset.UtcNow;

        var usersByIndex = new Dictionary<int, User>();
        foreach (var spec in DemoSeedCatalog.Users)
        {
            var user = new User
            {
                Id = SeedUserId(spec.Index),
                TelegramId = spec.TelegramId,
                TelegramUsername = spec.Username,
                Status = UserStatus.Active,
                Name = spec.FullName,
                BirthDate = spec.BirthDate,
                Gender = spec.Gender,
                CityId = minsk.Id,
                Coordinates = minsk.Coordinates,
                DatingGoal = spec.DatingGoal,
                Bio = spec.Bio,
                ProfileCompleteness = 100,
                SparksBalance = StartingSparksBalance,
                Locale = "ru",
                LastActiveAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            };

            dbContext.Users.Add(user);
            usersByIndex[spec.Index] = user;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var spec in DemoSeedCatalog.Users)
        {
            var user = usersByIndex[spec.Index];
            foreach (var interestIndex in spec.InterestIndexes)
            {
                dbContext.UserInterests.Add(new UserInterest
                {
                    UserId = user.Id,
                    InterestId = InterestSeed.All[interestIndex].Id,
                    CreatedAt = now,
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var mainPhotoUrlByIndex = new Dictionary<int, string?>();
        foreach (var spec in DemoSeedCatalog.Users)
        {
            var user = usersByIndex[spec.Index];
            string? mainPhotoUrl = null;
            for (var photoIndex = 0; photoIndex < spec.PhotoCount; photoIndex++)
            {
                var bytes = DemoPlaceholderImageGenerator.Generate(spec.Index, photoIndex);
                await using var content = new MemoryStream(bytes);
                var photo = await mediator.Send(
                    new UploadPhotoCommand(user.Id, content, "image/jpeg", bytes.Length), cancellationToken);

                if (photo.IsMain)
                {
                    mainPhotoUrl = photo.Url;
                }
            }

            mainPhotoUrlByIndex[spec.Index] = mainPhotoUrl;
        }

        foreach (var (indexA, indexB, contactUnlocked, archived) in MatchPairs)
        {
            var userA = usersByIndex[indexA];
            var userB = usersByIndex[indexB];

            dbContext.Swipes.Add(NewSwipe(userA.Id, userB.Id, now));
            dbContext.Swipes.Add(NewSwipe(userB.Id, userA.Id, now));

            var (user1Id, user2Id) = Canonicalize(userA.Id, userB.Id);
            var match = new Match
            {
                Id = Guid.NewGuid(),
                User1Id = user1Id,
                User2Id = user2Id,
                Status = archived ? MatchStatus.Archived : MatchStatus.Active,
                MatchedAt = now.AddDays(-3),
            };

            if (contactUnlocked)
            {
                match.ContactUnlockedAt = now.AddDays(-2);
                match.ContactUnlockedByUserId = userA.Id;
            }

            if (archived)
            {
                match.ArchivedAt = now.AddDays(-1);
                match.ArchivedReason = MatchArchivalPolicy.ManualArchivedReason;
            }

            dbContext.Matches.Add(match);
        }

        foreach (var (fromIndex, toIndex) in OneDirectionalLikes)
        {
            dbContext.Swipes.Add(NewSwipe(usersByIndex[fromIndex].Id, usersByIndex[toIndex].Id, now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return DemoSeedCatalog.Users
            .Select(spec => new DemoSeedResultUser(
                spec.TelegramId, spec.Username, spec.FullName, mainPhotoUrlByIndex[spec.Index]))
            .ToList();
    }

    private async Task DeleteExistingAsync(IReadOnlyList<long> telegramIds, CancellationToken cancellationToken)
    {
        var existingIds = await dbContext.Users
            .Where(u => telegramIds.Contains(u.TelegramId))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (existingIds.Count == 0)
        {
            return;
        }

        // Match/Swipe ссылаются на User с DeleteBehavior.Restrict (MatchConfiguration/SwipeConfiguration) —
        // удаляем их явно и раньше User. Photo/UserInterest каскадно удаляются вместе с User на уровне БД
        // (UserConfiguration: DeleteBehavior.Cascade), отдельного шага для них не нужно.
        await dbContext.Matches
            .Where(m => existingIds.Contains(m.User1Id) || existingIds.Contains(m.User2Id)
                || (m.ContactUnlockedByUserId != null && existingIds.Contains(m.ContactUnlockedByUserId.Value)))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Swipes
            .Where(s => existingIds.Contains(s.FromUserId) || existingIds.Contains(s.ToUserId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Users
            .Where(u => existingIds.Contains(u.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static Swipe NewSwipe(Guid fromUserId, Guid toUserId, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        FromUserId = fromUserId,
        ToUserId = toUserId,
        Type = SwipeType.Like,
        CreatedAt = now.AddDays(-3),
    };

    private static (Guid User1Id, Guid User2Id) Canonicalize(Guid a, Guid b) =>
        a.CompareTo(b) <= 0 ? (a, b) : (b, a);

    private static Guid SeedUserId(int index) => Guid.Parse($"00000000-0000-0000-0a10-{index:000000000000}");
}
