using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Blocks;

namespace Blizka.UnitTests.UseCases.Blocks;

public sealed class GetBlockedUsersQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА у пользователя есть блокировки ТОГДА возвращается список с главным фото")]
    public async Task Handle_returns_blocked_users_with_main_photo()
    {
        var blockerId = Guid.NewGuid();
        var blockedAt = DateTimeOffset.UtcNow;
        var blockedUser = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 1,
            Name = "Аня",
            Photos =
            [
                new Photo { Id = Guid.NewGuid(), Url = "https://example.com/not-main.jpg", IsMain = false },
                new Photo { Id = Guid.NewGuid(), Url = "https://example.com/main.jpg", IsMain = true },
            ],
        };
        var repository = new FakeUserBlockRepository(
            new UserBlock { Id = Guid.NewGuid(), BlockerUserId = blockerId, BlockedUserId = blockedUser.Id, BlockedUser = blockedUser, CreatedAt = blockedAt });
        var handler = new GetBlockedUsersQueryHandler(repository);

        var result = await handler.Handle(new GetBlockedUsersQuery(blockerId), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(blockedUser.Id, item.UserId);
        Assert.Equal("Аня", item.Name);
        Assert.Equal("https://example.com/main.jpg", item.MainPhotoUrl);
        Assert.Equal(blockedAt, item.BlockedAt);
    }

    [Fact(DisplayName = "КОГДА блокировок нет ТОГДА возвращается пустой список")]
    public async Task Handle_returns_empty_list_when_there_are_no_blocks()
    {
        var handler = new GetBlockedUsersQueryHandler(new FakeUserBlockRepository());

        var result = await handler.Handle(new GetBlockedUsersQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(result);
    }

    private sealed class FakeUserBlockRepository(params UserBlock[] seed) : IUserBlockRepository
    {
        private readonly List<UserBlock> _blocks = [.. seed];

        public Task<bool> ExistsAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetBlockedUsersQueryHandler.");

        public Task<bool> ExistsEitherDirectionAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetBlockedUsersQueryHandler.");

        public Task<IReadOnlyList<UserBlock>> GetBlockedByUserAsync(Guid blockerUserId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UserBlock>>(_blocks.Where(b => b.BlockerUserId == blockerUserId).ToList());

        public Task AddAsync(UserBlock block, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetBlockedUsersQueryHandler.");

        public Task RemoveAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetBlockedUsersQueryHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetBlockedUsersQueryHandler.");
    }
}
