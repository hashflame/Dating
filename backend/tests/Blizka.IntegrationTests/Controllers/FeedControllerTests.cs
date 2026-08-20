using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

/// <summary>Проверяет <see cref="Blizka.Api.Controllers.FeedController"/> (T-5.1) по тому же минимальному тестовому хосту, что и <see cref="PhotosControllerTests"/>.</summary>
public sealed class FeedControllerTests : IAsyncLifetime
{
    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeFeedRepository _feedRepository = null!;

    public async Task InitializeAsync()
    {
        _feedRepository = new FakeFeedRepository();

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
                    });
                });
                webBuilder.ConfigureServices((context, services) =>
                {
                    services.AddApiLayer(context.Configuration);
                    services.AddAppLayer();
                    services.AddSingleton<IFeedRepository>(_feedRepository);
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
}
