using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Feed;
using Blizka.App;
using Blizka.App.Auth;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Geometries;

namespace Blizka.IntegrationTests.Controllers;

/// <summary>Проверяет <see cref="Blizka.Api.Controllers.FeedController"/> (T-5.1, T-5.2) по тому же минимальному тестовому хосту, что и <see cref="PhotosControllerTests"/>.</summary>
public sealed class FeedControllerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = CreateResponseJsonOptions();

    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeFeedRepository _feedRepository = null!;
    private FakeUserRepository _userRepository = null!;
    private FakeSwipeRepository _swipeRepository = null!;
    private FakeMatchRepository _matchRepository = null!;

    public async Task InitializeAsync()
    {
        _feedRepository = new FakeFeedRepository();
        _userRepository = new FakeUserRepository();
        _swipeRepository = new FakeSwipeRepository();
        _matchRepository = new FakeMatchRepository();

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
                        ["Sparks:SuperlikeCost"] = "5",
                    });
                });
                webBuilder.ConfigureServices((context, services) =>
                {
                    services.AddApiLayer(context.Configuration);
                    services.AddAppLayer(context.Configuration);
                    services.AddSingleton<IFeedRepository>(_feedRepository);
                    services.AddSingleton<IUserRepository>(_userRepository);
                    services.AddSingleton<ISwipeRepository>(_swipeRepository);
                    services.AddSingleton<IMatchRepository>(_matchRepository);
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА лента отклоняется с 401")]
    public async Task GetFeed_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/feed");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА у пользователя есть город и кандидаты ТОГДА ответ 200 содержит карточку с совместимостью")]
    public async Task GetFeed_returns_a_card_for_the_matching_candidate()
    {
        var currentUserId = Guid.NewGuid();
        var cityId = Guid.NewGuid();
        _feedRepository.CurrentUser = CreateUser(currentUserId, cityId, Gender.Male);
        var candidate = CreateUser(Guid.NewGuid(), cityId, Gender.Female, "Anna");
        candidate.Photos.Add(new Photo
        {
            Id = Guid.NewGuid(), UserId = candidate.Id, Url = "u", ThumbnailUrl = "t", MediumUrl = "m", IsMain = true,
        });
        _feedRepository.Candidates = [candidate];
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/feed?limit=5");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<FeedResponse>>();
        Assert.False(body!.Data.Exhausted);
        var card = Assert.Single(body.Data.Items);
        Assert.Equal(candidate.Id, card.UserId);
        Assert.Equal("Anna", card.Name);
        Assert.Single(card.Photos);
        Assert.InRange(card.CompatibilityScore, 0, 100);
    }

    [Fact(DisplayName = "КОГДА кандидатов нет ТОГДА ответ 200 с пустым списком и exhausted true")]
    public async Task GetFeed_returns_an_exhausted_empty_list_when_there_are_no_candidates()
    {
        var currentUserId = Guid.NewGuid();
        _feedRepository.CurrentUser = CreateUser(currentUserId, Guid.NewGuid(), Gender.Male);
        _feedRepository.Candidates = [];
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/feed");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<FeedResponse>>();
        Assert.Empty(body!.Data.Items);
        Assert.True(body.Data.Exhausted);
    }

    [Fact(DisplayName = "КОГДА limit вне диапазона 1-50 ТОГДА ответ 400 VALIDATION_ERROR")]
    public async Task GetFeed_with_an_out_of_range_limit_returns_400_validation_error()
    {
        var currentUserId = Guid.NewGuid();
        _feedRepository.CurrentUser = CreateUser(currentUserId, Guid.NewGuid(), Gender.Male);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/feed?limit=999");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА лайк оказался взаимным ТОГДА ответ 200 с isMatch true и тремя icebreakers")]
    public async Task Like_returns_a_match_when_the_like_is_mutual()
    {
        var currentUserId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _userRepository.Users[currentUserId] = CreateUser(currentUserId, Guid.NewGuid(), Gender.Male);
        _userRepository.Users[targetId] = CreateUser(targetId, Guid.NewGuid(), Gender.Female, "Anna");
        _swipeRepository.HasMutualLike = true;
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/feed/{targetId}/like");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SwipeResponse>>(ResponseJsonOptions);
        Assert.True(body!.Data.IsMatch);
        Assert.NotNull(body.Data.Match);
        Assert.Equal("Anna", body.Data.Match.Name);
        Assert.Equal(3, body.Data.Match.Icebreakers.Length);
    }

    [Fact(DisplayName = "КОГДА цель свайпа не найдена ТОГДА ответ 404 SWIPE_TARGET_NOT_FOUND")]
    public async Task Dislike_returns_404_when_the_target_does_not_exist()
    {
        var currentUserId = Guid.NewGuid();
        _userRepository.Users[currentUserId] = CreateUser(currentUserId, Guid.NewGuid(), Gender.Male);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/feed/{Guid.NewGuid()}/dislike");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("SWIPE_TARGET_NOT_FOUND", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА пара уже свайпнута ТОГДА ответ 409 ALREADY_SWIPED")]
    public async Task Like_returns_409_when_already_swiped()
    {
        var currentUserId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _userRepository.Users[currentUserId] = CreateUser(currentUserId, Guid.NewGuid(), Gender.Male);
        _userRepository.Users[targetId] = CreateUser(targetId, Guid.NewGuid(), Gender.Female);
        _swipeRepository.AlreadyActive = true;
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/feed/{targetId}/like");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("ALREADY_SWIPED", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА недостаточно зорок на суперлайк ТОГДА ответ 402 INSUFFICIENT_SPARKS")]
    public async Task Superlike_returns_402_when_the_balance_is_insufficient()
    {
        var currentUserId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var currentUser = CreateUser(currentUserId, Guid.NewGuid(), Gender.Male);
        currentUser.SparksBalance = 0;
        _userRepository.Users[currentUserId] = currentUser;
        _userRepository.Users[targetId] = CreateUser(targetId, Guid.NewGuid(), Gender.Female);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/feed/{targetId}/superlike");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("INSUFFICIENT_SPARKS", body!.Error.Code);
    }

    private static User CreateUser(Guid id, Guid cityId, Gender gender, string name = "User") => new()
    {
        Id = id,
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = name,
        BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
        Gender = gender,
        CityId = cityId,
        City = new City
        {
            Id = cityId,
            NameRu = "Минск",
            NameBe = "Мінск",
            NameEn = "Minsk",
            Country = "BY",
            Coordinates = new Point(27.5667, 53.9),
            IsOpen = true,
            CreatedAt = DateTimeOffset.UtcNow,
        },
        Coordinates = new Point(27.5667, 53.9),
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

    private sealed class FakeFeedRepository : IFeedRepository
    {
        public User? CurrentUser { get; set; }

        public IReadOnlyList<User> Candidates { get; set; } = [];

        public Task<User?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(CurrentUser);

        public Task<IReadOnlyList<User>> GetCandidatesAsync(
            Guid currentUserId, Guid cityId, Gender preferredGender, int poolSize, CancellationToken cancellationToken) =>
            Task.FromResult(Candidates);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Dictionary<Guid, User> Users { get; } = [];

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах свайпа.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах свайпа.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Users.GetValueOrDefault(id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах свайпа.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeSwipeRepository : ISwipeRepository
    {
        public bool AlreadyActive { get; set; }

        public bool HasMutualLike { get; set; }

        public Task<bool> ExistsActiveAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
            Task.FromResult(AlreadyActive);

        public Task<bool> HasActiveMutualLikeAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
            Task.FromResult(HasMutualLike);

        public Task AddAsync(Swipe swipe, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeMatchRepository : IMatchRepository
    {
        public Task AddAsync(Match match, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeSparkTransactionRepository : ISparkTransactionRepository
    {
        public Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
