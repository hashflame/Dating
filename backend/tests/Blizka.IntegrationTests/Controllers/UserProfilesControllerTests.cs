using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Users;
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

namespace Blizka.IntegrationTests.Controllers;

/// <summary>
/// Проверяет <see cref="Blizka.Api.Controllers.UserProfilesController"/> — открыть чужую анкету по id
/// (ClickUp: из списков лайков, T-6.1, видно только userId/name/age/mainPhotoUrl) через реальный HTTP-конвейер,
/// по тому же минимальному тестовому хосту, что и <see cref="UsersControllerTests"/>.
/// </summary>
public sealed class UserProfilesControllerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = CreateResponseJsonOptions();

    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeUserRepository _userRepository = null!;

    public async Task InitializeAsync()
    {
        _userRepository = new FakeUserRepository();

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
                    services.AddAppLayer(context.Configuration);
                    services.AddSingleton<IUserRepository>(_userRepository);
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА GET /api/users/{userId} отклоняется с 401")]
    public async Task GetProfile_without_token_returns_401()
    {
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА пользователь существует ТОГДА GET /api/users/{userId} возвращает его анкету")]
    public async Task GetProfile_returns_the_target_users_profile()
    {
        var target = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 777,
            Name = "Anna",
            Status = UserStatus.Active,
            BirthDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date.AddYears(-30)),
        };
        _userRepository.Users[target.Id] = target;
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{target.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid()));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileResponse>>(ResponseJsonOptions);
        Assert.Equal(target.Id, body!.Data.UserId);
        Assert.Equal("Anna", body.Data.Name);
        Assert.Equal(30, body.Data.Age);
    }

    [Fact(DisplayName = "КОГДА пользователь не существует ТОГДА GET /api/users/{userId} отвечает 404 USER_PROFILE_NOT_FOUND")]
    public async Task GetProfile_returns_404_when_the_user_does_not_exist()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid()));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("USER_PROFILE_NOT_FOUND", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА аккаунт удалён ТОГДА GET /api/users/{userId} отвечает 404 — удалённый профиль недоступен по ссылке")]
    public async Task GetProfile_returns_404_for_a_deleted_account()
    {
        var deleted = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 555,
            Name = "Gone",
            Status = UserStatus.Deleted,
            BirthDate = new DateOnly(1995, 1, 1),
        };
        _userRepository.Users[deleted.Id] = deleted;
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{deleted.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid()));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("USER_PROFILE_NOT_FOUND", body!.Error.Code);
    }

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
            throw new NotSupportedException("Не используется в тестах анкеты пользователя.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Users.GetValueOrDefault(id));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах анкеты пользователя.");

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах анкеты пользователя.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
