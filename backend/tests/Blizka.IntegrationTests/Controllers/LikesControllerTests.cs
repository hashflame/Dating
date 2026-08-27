using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Likes;
using Blizka.App;
using Blizka.App.Auth;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace Blizka.IntegrationTests.Controllers;

/// <summary>Проверяет <see cref="Blizka.Api.Controllers.LikesController"/> (T-6.1) по тому же минимальному тестовому хосту, что и <see cref="FeedControllerTests"/>.</summary>
public sealed class LikesControllerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = CreateResponseJsonOptions();

    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeUserRepository _userRepository = null!;
    private FakeLikesRepository _likesRepository = null!;
    private FakePhotoStorageService _photoStorage = null!;

    public async Task InitializeAsync()
    {
        _userRepository = new FakeUserRepository();
        _likesRepository = new FakeLikesRepository();
        _photoStorage = new FakePhotoStorageService();

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
                        ["Sparks:LikesRevealCost"] = "10",
                    });
                });
                webBuilder.ConfigureServices((context, services) =>
                {
                    services.AddApiLayer(context.Configuration);
                    services.AddAppLayer(context.Configuration);
                    services.AddSingleton<IUserRepository>(_userRepository);
                    services.AddSingleton<ILikesRepository>(_likesRepository);
                    services.AddSingleton<IPhotoStorageService>(_photoStorage);
                    services.AddSingleton<ISparkTransactionRepository>(new FakeSparkTransactionRepository());
                    services.AddSingleton<IPrivacySettingsRepository>(new FakePrivacySettingsRepository());
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА входящие лайки отклоняются с 401")]
    public async Task GetIncoming_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/likes/incoming");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА список ещё не разблокирован ТОГДА GET /incoming возвращает count и заблюренное превью, без users")]
    public async Task GetIncoming_returns_blurred_preview_when_not_revealed()
    {
        var currentUserId = Guid.NewGuid();
        _userRepository.Users[currentUserId] = CreateUser(currentUserId, likesRevealed: false);
        var liker = CreateUser(Guid.NewGuid());
        liker.Photos.Add(CreatePhoto(liker.Id));
        _likesRepository.IncomingCount = 5;
        _likesRepository.IncomingPreview = [new LikeEntry(liker, DateTimeOffset.UtcNow)];
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/likes/incoming");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IncomingLikesResponse>>(ResponseJsonOptions);
        Assert.Equal(5, body!.Data.Count);
        Assert.False(body.Data.Revealed);
        Assert.Equal(10, body.Data.UnlockCost);
        Assert.Null(body.Data.Users);
        var preview = Assert.Single(body.Data.Preview!);
        Assert.StartsWith("data:image/jpeg;base64,", preview.BlurredPhotoUrl);
    }

    [Fact(DisplayName = "КОГДА список уже разблокирован ТОГДА GET /incoming возвращает полный список users")]
    public async Task GetIncoming_returns_the_full_list_when_already_revealed()
    {
        var currentUserId = Guid.NewGuid();
        _userRepository.Users[currentUserId] = CreateUser(currentUserId, likesRevealed: true);
        var liker = CreateUser(Guid.NewGuid(), "Anna");
        _likesRepository.Incoming = [new LikeEntry(liker, DateTimeOffset.UtcNow)];
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/likes/incoming");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IncomingLikesResponse>>(ResponseJsonOptions);
        Assert.True(body!.Data.Revealed);
        Assert.Null(body.Data.Preview);
        var user = Assert.Single(body.Data.Users!);
        Assert.Equal("Anna", user.Name);
    }

    [Fact(DisplayName = "КОГДА запрос GET /outgoing без токена ТОГДА ответ 401")]
    public async Task GetOutgoing_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/likes/outgoing");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА есть исходящие лайки ТОГДА GET /outgoing возвращает их полный список")]
    public async Task GetOutgoing_returns_the_full_list()
    {
        var currentUserId = Guid.NewGuid();
        _userRepository.Users[currentUserId] = CreateUser(currentUserId);
        var liked = CreateUser(Guid.NewGuid(), "Anna");
        _likesRepository.Outgoing = [new LikeEntry(liked, DateTimeOffset.UtcNow)];
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/likes/outgoing");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<OutgoingLikesResponse>>(ResponseJsonOptions);
        Assert.Equal(1, body!.Data.Count);
        Assert.Equal("Anna", body.Data.Users[0].Name);
    }

    [Fact(DisplayName = "КОГДА баланса на разблокировку не хватает ТОГДА POST /incoming/reveal возвращает 402 INSUFFICIENT_SPARKS")]
    public async Task Reveal_returns_402_when_the_balance_is_insufficient()
    {
        var currentUserId = Guid.NewGuid();
        var currentUser = CreateUser(currentUserId, likesRevealed: false);
        currentUser.SparksBalance = 0;
        _userRepository.Users[currentUserId] = currentUser;
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/likes/incoming/reveal");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("INSUFFICIENT_SPARKS", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА баланса хватает ТОГДА POST /incoming/reveal списывает зорки и возвращает полный список")]
    public async Task Reveal_spends_sparks_and_returns_the_full_list()
    {
        var currentUserId = Guid.NewGuid();
        var currentUser = CreateUser(currentUserId, likesRevealed: false);
        currentUser.SparksBalance = 20;
        _userRepository.Users[currentUserId] = currentUser;
        var liker = CreateUser(Guid.NewGuid(), "Anna");
        _likesRepository.Incoming = [new LikeEntry(liker, DateTimeOffset.UtcNow)];
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/likes/incoming/reveal");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RevealIncomingLikesResponse>>(ResponseJsonOptions);
        Assert.Equal(10, body!.Data.SparksSpent);
        Assert.Equal(10, body.Data.SparksBalance);
        Assert.Single(body.Data.Users);
        Assert.True(currentUser.LikesRevealed);
    }

    [Fact(DisplayName = "КОГДА список уже был разблокирован ТОГДА повторный POST /incoming/reveal идемпотентен — зорки не списываются повторно")]
    public async Task Reveal_is_idempotent_on_a_second_call()
    {
        var currentUserId = Guid.NewGuid();
        var currentUser = CreateUser(currentUserId, likesRevealed: true);
        currentUser.SparksBalance = 20;
        _userRepository.Users[currentUserId] = currentUser;
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/likes/incoming/reveal");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RevealIncomingLikesResponse>>(ResponseJsonOptions);
        Assert.Equal(0, body!.Data.SparksSpent);
        Assert.Equal(20, body.Data.SparksBalance);
    }

    private static User CreateUser(Guid id, string name = "User", bool likesRevealed = false) => new()
    {
        Id = id,
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = name,
        BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
        Gender = Gender.Female,
        Locale = "ru",
        LikesRevealed = likesRevealed,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static Photo CreatePhoto(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Url = "https://cdn.test/original.jpg",
        ThumbnailUrl = "https://cdn.test/thumbnail.jpg",
        MediumUrl = "https://cdn.test/medium.jpg",
        IsMain = true,
        CreatedAt = DateTimeOffset.UtcNow,
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
            throw new NotSupportedException("Не используется в тестах списков лайков.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Users.GetValueOrDefault(id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeLikesRepository : ILikesRepository
    {
        public int IncomingCount { get; set; }

        public IReadOnlyList<LikeEntry> IncomingPreview { get; set; } = [];

        public IReadOnlyList<LikeEntry> Incoming { get; set; } = [];

        public IReadOnlyList<LikeEntry> Outgoing { get; set; } = [];

        public Task<int> CountIncomingAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(IncomingCount);

        public Task<IReadOnlyList<LikeEntry>> GetIncomingPreviewAsync(Guid userId, int limit, CancellationToken cancellationToken) =>
            Task.FromResult(IncomingPreview);

        public Task<IReadOnlyList<LikeEntry>> GetIncomingAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Incoming);

        public Task<IReadOnlyList<LikeEntry>> GetOutgoingAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Outgoing);
    }

    private sealed class FakePhotoStorageService : IPhotoStorageService
    {
        public Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");

        public Task<byte[]> DownloadAsync(string key, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            using (var image = new Image<Rgba32>(10, 10))
            {
                image.Save(buffer, new JpegEncoder());
            }

            return Task.FromResult(buffer.ToArray());
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");

        public Task<string> GetTemporaryDownloadUrlAsync(string key, TimeSpan validFor, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");
    }

    private sealed class FakeSparkTransactionRepository : ISparkTransactionRepository
    {
        public Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<(IReadOnlyList<SparkTransaction> Items, int TotalCount)> GetHistoryAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<SparkTransaction>, int)>(([], 0));

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePrivacySettingsRepository : IPrivacySettingsRepository
    {
        public Task<PrivacySettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<PrivacySettings?>(null);

        public Task<PrivacySettings?> GetByUserIdTrackedAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<PrivacySettings?>(null);

        public Task<IReadOnlyDictionary<Guid, PrivacySettings>> GetByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, PrivacySettings>>(new Dictionary<Guid, PrivacySettings>());

        public Task AddAsync(PrivacySettings settings, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
