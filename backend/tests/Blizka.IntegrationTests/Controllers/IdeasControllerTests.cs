using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Ideas;
using Blizka.App;
using Blizka.App.Auth;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Blizka.IntegrationTests.Controllers;

/// <summary>Проверяет <see cref="Blizka.Api.Controllers.IdeasController"/> (T-19.1) по тому же минимальному тестовому хосту, что и <see cref="LikesControllerTests"/>.</summary>
public sealed class IdeasControllerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = CreateResponseJsonOptions();

    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeUserRepository _userRepository = null!;
    private FakeIdeaRepository _ideaRepository = null!;

    public async Task InitializeAsync()
    {
        _userRepository = new FakeUserRepository();
        _ideaRepository = new FakeIdeaRepository();

        _host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = "test-only-secret-not-used-outside-this-test-host",
                        ["Jwt:Issuer"] = "blizka-tests",
                        ["Jwt:Audience"] = "blizka-tests-clients",
                        ["Sparks:IdeaSubmissionBonusAmount"] = "1",
                    });
                });
                webBuilder.ConfigureServices((context, services) =>
                {
                    services.AddApiLayer(context.Configuration);
                    services.AddAppLayer(context.Configuration);
                    services.AddSingleton<IUserRepository>(_userRepository);
                    services.AddSingleton<IIdeaRepository>(_ideaRepository);
                    services.AddSingleton<ISparkTransactionRepository>(new FakeSparkTransactionRepository());
                    services.AddExceptionHandler<BlizkaExceptionHandler>();
                    services.AddProblemDetails();
                });
                webBuilder.Configure(app =>
                {
                    app.UseExceptionHandler();
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                });
            })
            .StartAsync();

        _client = _host.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА GET /api/ideas отклоняется с 401")]
    public async Task GetIdeas_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/ideas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА идея анонимна ТОГДА в ответе GET /api/ideas authorName null")]
    public async Task GetIdeas_hides_the_author_name_for_anonymous_ideas()
    {
        var currentUserId = Guid.NewGuid();
        _userRepository.Users[currentUserId] = CreateUser(currentUserId);
        var idea = new Idea
        {
            Id = Guid.NewGuid(),
            AuthorUserId = Guid.NewGuid(),
            AuthorUser = CreateUser(Guid.NewGuid(), "Anna"),
            Text = "Add a spectator mode",
            IsAnonymous = true,
            Status = IdeaStatus.New,
            VotesCount = 5,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _ideaRepository.Page = ([new IdeaListEntry(idea, HasVoted: false)], 1);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/ideas?tab=hot");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResponse<IdeaDto>>>(ResponseJsonOptions);
        var item = Assert.Single(body!.Data.Items);
        Assert.Null(item.AuthorName);
        Assert.Equal("new", item.Status);
        Assert.Equal(5, item.VotesCount);
    }

    [Fact(DisplayName = "КОГДА tab невалиден ТОГДА GET /api/ideas отвечает 400 VALIDATION_ERROR")]
    public async Task GetIdeas_returns_400_for_an_invalid_tab()
    {
        var currentUserId = Guid.NewGuid();
        _userRepository.Users[currentUserId] = CreateUser(currentUserId);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/ideas?tab=bogus");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА идея отправлена впервые в этом месяце ТОГДА POST /api/ideas начисляет зорку")]
    public async Task CreateIdea_awards_the_monthly_bonus()
    {
        var currentUserId = Guid.NewGuid();
        _userRepository.Users[currentUserId] = CreateUser(currentUserId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/ideas")
        {
            Content = JsonContent.Create(new { text = "Add dark mode", anonymous = false }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CreateIdeaResponse>>(ResponseJsonOptions);
        Assert.Equal(1, body!.Data.SparksAwarded);
        Assert.Equal("new", body.Data.Status);
        Assert.Single(_ideaRepository.Added);
    }

    [Fact(DisplayName = "КОГДА идеи с таким id нет ТОГДА POST /api/ideas/{id}/vote отвечает 404")]
    public async Task Vote_returns_404_when_the_idea_does_not_exist()
    {
        var currentUserId = Guid.NewGuid();
        _userRepository.Users[currentUserId] = CreateUser(currentUserId);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/ideas/{Guid.NewGuid()}/vote");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("IDEA_NOT_FOUND", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА идея существует ТОГДА POST /api/ideas/{id}/vote отвечает 204 и ставит голос")]
    public async Task Vote_returns_204_and_records_the_vote()
    {
        var currentUserId = Guid.NewGuid();
        _userRepository.Users[currentUserId] = CreateUser(currentUserId);
        var ideaId = Guid.NewGuid();
        _ideaRepository.ExistingIdeaIds.Add(ideaId);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/ideas/{ideaId}/vote");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains((ideaId, currentUserId), _ideaRepository.Votes);
    }

    [Fact(DisplayName = "КОГДА голос снимается ТОГДА DELETE /api/ideas/{id}/vote отвечает 204 без проверки существования идеи")]
    public async Task RemoveVote_returns_204()
    {
        var currentUserId = Guid.NewGuid();
        _userRepository.Users[currentUserId] = CreateUser(currentUserId);
        var ideaId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/ideas/{ideaId}/vote");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains((ideaId, currentUserId), _ideaRepository.RemovedVotes);
    }

    private static User CreateUser(Guid id, string name = "User") => new()
    {
        Id = id,
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = name,
        BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
        Gender = Gender.Female,
        Locale = "ru",
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static JsonSerializerOptions CreateResponseJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private string IssueToken(Guid userId)
    {
        var jwtTokenService = _host.Services.GetRequiredService<IJwtTokenService>();
        var user = new User { Id = userId, TelegramId = 1, Locale = "ru", Status = UserStatus.Active };
        return jwtTokenService.IssueToken(user).Token;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Dictionary<Guid, User> Users { get; } = [];

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах доски идей.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах доски идей.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Users.GetValueOrDefault(id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах доски идей.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeIdeaRepository : IIdeaRepository
    {
        public (IReadOnlyList<IdeaListEntry> Items, int TotalCount) Page { get; set; } = ([], 0);

        public HashSet<Guid> ExistingIdeaIds { get; } = [];

        public List<Idea> Added { get; } = [];

        public List<(Guid IdeaId, Guid UserId)> Votes { get; } = [];

        public List<(Guid IdeaId, Guid UserId)> RemovedVotes { get; } = [];

        public Task<(IReadOnlyList<IdeaListEntry> Items, int TotalCount)> GetPageAsync(
            IdeaListTab tab, Guid currentUserId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(Page);

        public Task<bool> ExistsAsync(Guid ideaId, CancellationToken cancellationToken) =>
            Task.FromResult(ExistingIdeaIds.Contains(ideaId));

        public Task AddAsync(Idea idea, CancellationToken cancellationToken)
        {
            Added.Add(idea);
            return Task.CompletedTask;
        }

        public Task<bool> AddVoteAsync(Guid ideaId, Guid userId, CancellationToken cancellationToken)
        {
            Votes.Add((ideaId, userId));
            return Task.FromResult(true);
        }

        public Task<bool> RemoveVoteAsync(Guid ideaId, Guid userId, CancellationToken cancellationToken)
        {
            RemovedVotes.Add((ideaId, userId));
            return Task.FromResult(true);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeSparkTransactionRepository : ISparkTransactionRepository
    {
        public Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<(IReadOnlyList<SparkTransaction> Items, int TotalCount)> GetHistoryAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<SparkTransaction>, int)>(([], 0));

        public Task<bool> ExistsSinceAsync(Guid userId, SparkTransactionType type, DateTimeOffset since, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
