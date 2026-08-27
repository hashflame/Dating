using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Notifications;
using FluentValidation;

namespace Blizka.UnitTests.UseCases.Notifications;

public sealed class MarkNotificationsSeenCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА likes=true ТОГДА LastSeenLikesAt выставляется, LastSeenMatchesAt не трогается")]
    public async Task Handle_marks_only_likes_seen()
    {
        var user = CreateUser();
        var repository = new FakeUserRepository(user);
        var handler = CreateHandler(repository);

        await handler.Handle(new MarkNotificationsSeenCommand(user.Id, Likes: true, Matches: false), CancellationToken.None);

        Assert.NotNull(user.LastSeenLikesAt);
        Assert.Null(user.LastSeenMatchesAt);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА matches=true ТОГДА LastSeenMatchesAt выставляется, LastSeenLikesAt не трогается")]
    public async Task Handle_marks_only_matches_seen()
    {
        var user = CreateUser();
        var repository = new FakeUserRepository(user);
        var handler = CreateHandler(repository);

        await handler.Handle(new MarkNotificationsSeenCommand(user.Id, Likes: false, Matches: true), CancellationToken.None);

        Assert.Null(user.LastSeenLikesAt);
        Assert.NotNull(user.LastSeenMatchesAt);
    }

    [Fact(DisplayName = "КОГДА оба флага true ТОГДА обе метки выставляются")]
    public async Task Handle_marks_both_seen()
    {
        var user = CreateUser();
        var repository = new FakeUserRepository(user);
        var handler = CreateHandler(repository);

        await handler.Handle(new MarkNotificationsSeenCommand(user.Id, Likes: true, Matches: true), CancellationToken.None);

        Assert.NotNull(user.LastSeenLikesAt);
        Assert.NotNull(user.LastSeenMatchesAt);
    }

    [Fact(DisplayName = "КОГДА оба флага false ТОГДА выбрасывается ValidationException, ничего не сохраняется")]
    public async Task Handle_throws_when_neither_flag_is_set()
    {
        var user = CreateUser();
        var repository = new FakeUserRepository(user);
        var handler = CreateHandler(repository);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new MarkNotificationsSeenCommand(user.Id, Likes: false, Matches: false), CancellationToken.None));
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА SaveChangesAsync падает на конкурентном сохранении ТОГДА выбрасывается NotificationsSeenConflictException")]
    public async Task Handle_translates_a_concurrent_save_race_into_NotificationsSeenConflictException()
    {
        var user = CreateUser();
        var repository = new FakeUserRepository(user)
        {
            SaveChangesFailsWith = new ConcurrentUserUpdateException(user.Id, new InvalidOperationException("simulated concurrency conflict")),
        };
        var handler = CreateHandler(repository);

        var exception = await Assert.ThrowsAsync<NotificationsSeenConflictException>(
            () => handler.Handle(new MarkNotificationsSeenCommand(user.Id, Likes: true, Matches: false), CancellationToken.None));
        Assert.Equal(user.Id, exception.UserId);
    }

    private static MarkNotificationsSeenCommandHandler CreateHandler(IUserRepository repository) =>
        new(repository, new MarkNotificationsSeenCommandValidator());

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = "Ann",
        BirthDate = new DateOnly(1995, 1, 1),
        Gender = Gender.Female,
        Locale = "ru",
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeUserRepository(User user) : IUserRepository
    {
        public bool SaveChangesCalled { get; private set; }

        public Exception? SaveChangesFailsWith { get; set; }

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отметки уведомлений просмотренными.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отметки уведомлений просмотренными.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id == id ? user : null);

        public Task AddAsync(User newUser, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отметки уведомлений просмотренными.");

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            if (SaveChangesFailsWith is { } exception)
            {
                SaveChangesFailsWith = null;
                throw exception;
            }

            return Task.CompletedTask;
        }
    }
}
