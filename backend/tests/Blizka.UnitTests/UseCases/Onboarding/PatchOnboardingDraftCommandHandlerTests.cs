using System.Text.Json;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Onboarding;
using FluentValidation;

namespace Blizka.UnitTests.UseCases.Onboarding;

public sealed class PatchOnboardingDraftCommandHandlerTests
{
    private static PatchOnboardingDraftCommandHandler CreateHandler(FakeOnboardingDraftRepository repository, bool cityExists = true) =>
        new(
            repository,
            new OnboardingStep1DataValidator(),
            new OnboardingStep2DataValidator(),
            new OnboardingStep3DataValidator(new FakeCityRepository(cityExists)));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact(DisplayName = "КОГДА пользователь впервые сохраняет шаг 1 ТОГДА создаётся черновик с этим шагом")]
    public async Task Handle_creates_a_new_draft_for_the_first_step()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeOnboardingDraftRepository();
        var handler = CreateHandler(repository);
        var data = Parse("""{"name":"Ann","birthDate":"2000-01-01","gender":"female"}""");

        var result = await handler.Handle(new PatchOnboardingDraftCommand(userId, 1, data), CancellationToken.None);

        Assert.Equal(1, result.Step);
        Assert.Equal("Ann", result.Data.GetProperty("name").GetString());
        var stored = Assert.Single(repository.Drafts);
        Assert.Equal(userId, stored.UserId);
    }

    [Fact(DisplayName = "КОГДА последовательно сохраняются шаги 1 и 2 ТОГДА данные обоих шагов накапливаются вместе")]
    public async Task Handle_accumulates_data_across_steps()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeOnboardingDraftRepository();
        var handler = CreateHandler(repository);

        await handler.Handle(
            new PatchOnboardingDraftCommand(userId, 1, Parse("""{"name":"Ann","birthDate":"2000-01-01","gender":"female"}""")),
            CancellationToken.None);
        var result = await handler.Handle(
            new PatchOnboardingDraftCommand(
                userId,
                2,
                Parse("""{"showGender":"male","ageRange":{"min":20,"max":35},"datingGoals":["casual"]}""")),
            CancellationToken.None);

        Assert.Equal(2, result.Step);
        Assert.Equal("Ann", result.Data.GetProperty("name").GetString());
        Assert.Equal("male", result.Data.GetProperty("showGender").GetString());
    }

    [Fact(DisplayName = "КОГДА шаг 2 сохраняется повторно ТОГДА новые данные перезаписывают только этот шаг")]
    public async Task Handle_overwrites_only_the_repeated_step()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeOnboardingDraftRepository();
        var handler = CreateHandler(repository);
        var step2Command = new Func<string, PatchOnboardingDraftCommand>(
            goal => new PatchOnboardingDraftCommand(
                userId,
                2,
                Parse($$"""{"showGender":"all","ageRange":{"min":18,"max":40},"datingGoals":["{{goal}}"]}""")));

        await handler.Handle(
            new PatchOnboardingDraftCommand(userId, 1, Parse("""{"name":"Ann","birthDate":"2000-01-01","gender":"female"}""")),
            CancellationToken.None);
        await handler.Handle(step2Command("casual"), CancellationToken.None);
        var result = await handler.Handle(step2Command("friendship"), CancellationToken.None);

        Assert.Equal("Ann", result.Data.GetProperty("name").GetString());
        var goals = result.Data.GetProperty("datingGoals").EnumerateArray().Select(g => g.GetString()).ToArray();
        Assert.Equal("friendship", Assert.Single(goals));
    }

    [Fact(DisplayName = "КОГДА пользователь возвращается к более раннему шагу ТОГДА отметка прогресса Step не регрессирует")]
    public async Task Handle_does_not_regress_the_step_marker_when_editing_an_earlier_step()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeOnboardingDraftRepository();
        var handler = CreateHandler(repository);

        await handler.Handle(
            new PatchOnboardingDraftCommand(userId, 1, Parse("""{"name":"Ann","birthDate":"2000-01-01","gender":"female"}""")),
            CancellationToken.None);
        await handler.Handle(
            new PatchOnboardingDraftCommand(
                userId, 2, Parse("""{"showGender":"all","ageRange":{"min":18,"max":40},"datingGoals":["casual"]}""")),
            CancellationToken.None);
        var result = await handler.Handle(
            new PatchOnboardingDraftCommand(userId, 1, Parse("""{"name":"Anna","birthDate":"2000-01-01","gender":"female"}""")),
            CancellationToken.None);

        Assert.Equal(2, result.Step);
        Assert.Equal("Anna", result.Data.GetProperty("name").GetString());
    }

    [Fact(DisplayName = "КОГДА данные шага не проходят валидацию ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_ValidationException_for_invalid_step_data()
    {
        var repository = new FakeOnboardingDraftRepository();
        var handler = CreateHandler(repository);
        var underage = Parse("""{"name":"Ann","birthDate":"2015-01-01","gender":"female"}""");

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new PatchOnboardingDraftCommand(Guid.NewGuid(), 1, underage), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА номер шага не поддерживается ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_ValidationException_for_an_unsupported_step_number()
    {
        var repository = new FakeOnboardingDraftRepository();
        var handler = CreateHandler(repository);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new PatchOnboardingDraftCommand(Guid.NewGuid(), 4, Parse("{}")), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА при первом сохранении черновика происходит гонка по UserId ТОГДА данные накладываются на уже созданный конкурентом черновик без ошибки")]
    public async Task Handle_recovers_from_a_concurrent_draft_creation_conflict()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeOnboardingDraftRepository();
        repository.ConcurrentWinner = new OnboardingDraft { UserId = userId, Step = 1, DataJson = """{"name":"Winner"}""" };
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new PatchOnboardingDraftCommand(userId, 1, Parse("""{"name":"Ann","birthDate":"2000-01-01","gender":"female"}""")),
            CancellationToken.None);

        Assert.Equal(1, result.Step);
        Assert.Equal("Ann", result.Data.GetProperty("name").GetString());
        Assert.Single(repository.Drafts);
    }

    private sealed class FakeCityRepository(bool exists) : ICityRepository
    {
        public Task<bool> ExistsAsync(Guid cityId, CancellationToken cancellationToken) => Task.FromResult(exists);
    }

    private sealed class FakeOnboardingDraftRepository : IOnboardingDraftRepository
    {
        private readonly List<OnboardingDraft> _pending = [];

        public List<OnboardingDraft> Drafts { get; } = [];

        /// <summary>Когда задано, следующий SaveChangesAsync симулирует конкурентную вставку черновика с тем же UserId — "чужой" черновик фиксируется первым, а наша попытка добавить новый падает с ConcurrentOnboardingDraftCreationException.</summary>
        public OnboardingDraft? ConcurrentWinner { get; set; }

        public Task<OnboardingDraft?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Drafts.SingleOrDefault(d => d.UserId == userId));

        public Task AddAsync(OnboardingDraft draft, CancellationToken cancellationToken)
        {
            _pending.Add(draft);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (ConcurrentWinner is { } winner)
            {
                ConcurrentWinner = null;
                Drafts.Add(winner);
                _pending.Clear();
                throw new ConcurrentOnboardingDraftCreationException(winner.UserId, new InvalidOperationException("simulated PK violation"));
            }

            Drafts.AddRange(_pending);
            _pending.Clear();
            return Task.CompletedTask;
        }
    }
}
