using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Blocks;
using Blizka.App.UseCases.Reports;
using FluentValidation;
using MediatR;

namespace Blizka.UnitTests.UseCases.Reports;

public sealed class CreateReportCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА подана обычная жалоба (spam) ТОГДА она сохраняется с приоритетом Normal и аккаунт не блокируется")]
    public async Task Handle_saves_a_normal_priority_report()
    {
        var reporter = Guid.NewGuid();
        var reported = new User { Id = Guid.NewGuid(), TelegramId = 1, Status = UserStatus.Active };
        var userRepository = new FakeUserRepository(reported);
        var reportRepository = new FakeReportRepository();
        var mediator = new FakeMediator();
        var handler = new CreateReportCommandHandler(userRepository, reportRepository, mediator, new CreateReportCommandValidator());

        await handler.Handle(new CreateReportCommand(reporter, reported.Id, ReportReason.Spam, "коммент", false), CancellationToken.None);

        var added = Assert.Single(reportRepository.AddedReports);
        Assert.Equal(reporter, added.ReporterUserId);
        Assert.Equal(reported.Id, added.ReportedUserId);
        Assert.Equal(ReportReason.Spam, added.Reason);
        Assert.Equal(ReportPriority.Normal, added.Priority);
        Assert.Equal(ReportStatus.Pending, added.Status);
        Assert.True(reportRepository.SaveChangesCalled);
        Assert.Equal(UserStatus.Active, reported.Status);
        Assert.Empty(mediator.SentRequests);
    }

    [Theory(DisplayName = "КОГДА причина жалобы критичная (underage/unsafe_meeting) ТОГДА аккаунт блокируется немедленно")]
    [InlineData(ReportReason.Underage)]
    [InlineData(ReportReason.UnsafeMeeting)]
    public async Task Handle_bans_immediately_for_critical_reasons(ReportReason reason)
    {
        var reported = new User { Id = Guid.NewGuid(), TelegramId = 1, Status = UserStatus.Active };
        var userRepository = new FakeUserRepository(reported);
        var reportRepository = new FakeReportRepository();
        var handler = new CreateReportCommandHandler(userRepository, reportRepository, new FakeMediator(), new CreateReportCommandValidator());

        await handler.Handle(new CreateReportCommand(Guid.NewGuid(), reported.Id, reason, null, false), CancellationToken.None);

        Assert.Equal(ReportPriority.Critical, Assert.Single(reportRepository.AddedReports).Priority);
        Assert.Equal(UserStatus.Banned, reported.Status);
        Assert.NotNull(reported.BanReason);
    }

    [Fact(DisplayName = "КОГДА причина жалобы критичная, но аккаунт уже удалён ТОГДА статус Deleted не перезаписывается")]
    public async Task Handle_does_not_override_deleted_status_for_critical_reports()
    {
        var reported = new User { Id = Guid.NewGuid(), TelegramId = 1, Status = UserStatus.Deleted };
        var userRepository = new FakeUserRepository(reported);
        var handler = new CreateReportCommandHandler(userRepository, new FakeReportRepository(), new FakeMediator(), new CreateReportCommandValidator());

        await handler.Handle(new CreateReportCommand(Guid.NewGuid(), reported.Id, ReportReason.Underage, null, false), CancellationToken.None);

        Assert.Equal(UserStatus.Deleted, reported.Status);
    }

    [Fact(DisplayName = "КОГДА blockUser=true ТОГДА одновременно отправляется BlockUserCommand")]
    public async Task Handle_sends_block_command_when_requested()
    {
        var reporter = Guid.NewGuid();
        var reported = new User { Id = Guid.NewGuid(), TelegramId = 1, Status = UserStatus.Active };
        var userRepository = new FakeUserRepository(reported);
        var mediator = new FakeMediator();
        var handler = new CreateReportCommandHandler(userRepository, new FakeReportRepository(), mediator, new CreateReportCommandValidator());

        await handler.Handle(new CreateReportCommand(reporter, reported.Id, ReportReason.Insults, null, true), CancellationToken.None);

        var sent = Assert.Single(mediator.SentRequests);
        var blockCommand = Assert.IsType<BlockUserCommand>(sent);
        Assert.Equal(reporter, blockCommand.BlockerUserId);
        Assert.Equal(reported.Id, blockCommand.BlockedUserId);
    }

    [Fact(DisplayName = "КОГДА цель жалобы не найдена ТОГДА выбрасывается UserProfileNotFoundException")]
    public async Task Handle_throws_when_the_target_does_not_exist()
    {
        var handler = new CreateReportCommandHandler(
            new FakeUserRepository(), new FakeReportRepository(), new FakeMediator(), new CreateReportCommandValidator());

        await Assert.ThrowsAsync<UserProfileNotFoundException>(
            () => handler.Handle(new CreateReportCommand(Guid.NewGuid(), Guid.NewGuid(), ReportReason.Spam, null, false), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА пользователь жалуется на самого себя ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_when_reporting_self()
    {
        var userId = Guid.NewGuid();
        var handler = new CreateReportCommandHandler(
            new FakeUserRepository(), new FakeReportRepository(), new FakeMediator(), new CreateReportCommandValidator());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new CreateReportCommand(userId, userId, ReportReason.Spam, null, false), CancellationToken.None));
    }

    private sealed class FakeUserRepository(params User[] seed) : IUserRepository
    {
        private readonly List<User> _users = [.. seed];

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах CreateReportCommandHandler.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах CreateReportCommandHandler.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах CreateReportCommandHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах CreateReportCommandHandler.");
    }

    private sealed class FakeReportRepository : IReportRepository
    {
        public List<Report> AddedReports { get; } = [];

        public bool SaveChangesCalled { get; private set; }

        public Task AddAsync(Report report, CancellationToken cancellationToken)
        {
            AddedReports.Add(report);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Guid>> GetUsersExceedingReportThresholdAsync(
            DateTimeOffset since, int thresholdCount, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах CreateReportCommandHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }

    /// <summary>Форвардит только <c>Send&lt;TRequest&gt;(TRequest)</c> — единственный член IMediator, используемый хендлером жалоб.</summary>
    private sealed class FakeMediator : IMediator
    {
        public List<object> SentRequests { get; } = [];

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            SentRequests.Add(request!);
            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification =>
            throw new NotSupportedException();
    }
}
