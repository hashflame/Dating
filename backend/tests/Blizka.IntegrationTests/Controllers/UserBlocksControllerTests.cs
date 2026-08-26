using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api;
using Blizka.Api.Blocks;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
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

/// <summary>Проверяет <see cref="Blizka.Api.Controllers.UserBlocksController"/> (T-16.2) по тому же минимальному тестовому хосту, что и <see cref="UsersControllerTests"/>.</summary>
public sealed class UserBlocksControllerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = CreateResponseJsonOptions();

    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeUserRepository _userRepository = null!;
    private FakeUserBlockRepository _blockRepository = null!;

    public async Task InitializeAsync()
    {
        _userRepository = new FakeUserRepository();
        _blockRepository = new FakeUserBlockRepository();

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
                    services.AddSingleton<IUserBlockRepository>(_blockRepository);
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА POST /api/users/{userId}/block отклоняется с 401")]
    public async Task Block_without_token_returns_401()
    {
        var response = await _client.PostAsync($"/api/users/{Guid.NewGuid()}/block", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА цель блокировки существует ТОГДА POST /api/users/{userId}/block возвращает 204 и сохраняет блокировку")]
    public async Task Block_with_valid_target_returns_204()
    {
        var blockerId = Guid.NewGuid();
        var token = IssueToken(blockerId);
        var target = new User { Id = Guid.NewGuid(), TelegramId = 2, Name = "Target" };
        _userRepository.Users.Add(target);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/users/{target.Id}/block");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(_blockRepository.Blocks, b => b.BlockerUserId == blockerId && b.BlockedUserId == target.Id);
    }

    [Fact(DisplayName = "КОГДА цель блокировки не найдена ТОГДА POST /api/users/{userId}/block возвращает 404")]
    public async Task Block_with_missing_target_returns_404()
    {
        var token = IssueToken(Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/users/{Guid.NewGuid()}/block");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА пользователь пытается заблокировать самого себя ТОГДА возвращается 400")]
    public async Task Block_self_returns_400()
    {
        var userId = Guid.NewGuid();
        var token = IssueToken(userId);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/users/{userId}/block");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА блокировка снимается ТОГДА DELETE /api/users/{userId}/block возвращает 204 и убирает её из списка")]
    public async Task Unblock_removes_the_block()
    {
        var blockerId = Guid.NewGuid();
        var token = IssueToken(blockerId);
        var blockedId = Guid.NewGuid();
        _blockRepository.Blocks.Add(new UserBlock { Id = Guid.NewGuid(), BlockerUserId = blockerId, BlockedUserId = blockedId, CreatedAt = DateTimeOffset.UtcNow });
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/users/{blockedId}/block");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.DoesNotContain(_blockRepository.Blocks, b => b.BlockerUserId == blockerId && b.BlockedUserId == blockedId);
    }

    [Fact(DisplayName = "КОГДА у пользователя есть блокировки ТОГДА GET /api/users/me/blocked возвращает их список")]
    public async Task GetBlocked_returns_the_list()
    {
        var blockerId = Guid.NewGuid();
        var token = IssueToken(blockerId);
        var blockedUser = new User { Id = Guid.NewGuid(), TelegramId = 3, Name = "Заблокированный" };
        _blockRepository.Blocks.Add(new UserBlock
        {
            Id = Guid.NewGuid(), BlockerUserId = blockerId, BlockedUserId = blockedUser.Id, BlockedUser = blockedUser, CreatedAt = DateTimeOffset.UtcNow,
        });
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me/blocked");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BlockedUserResponse[]>>(ResponseJsonOptions);
        var item = Assert.Single(body!.Data);
        Assert.Equal(blockedUser.Id, item.UserId);
        Assert.Equal("Заблокированный", item.Name);
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
        _userRepository.Users.RemoveAll(u => u.Id == userId);
        _userRepository.Users.Add(user);
        return jwtTokenService.IssueToken(user).Token;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            Task.FromResult(Users.SingleOrDefault(u => u.TelegramId == telegramId));

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Users.SingleOrDefault(u => u.Id == id));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUserBlockRepository : IUserBlockRepository
    {
        public List<UserBlock> Blocks { get; } = [];

        public Task<bool> ExistsAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken) =>
            Task.FromResult(Blocks.Any(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId));

        public Task<bool> ExistsEitherDirectionAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken) =>
            Task.FromResult(Blocks.Any(b =>
                (b.BlockerUserId == userId && b.BlockedUserId == otherUserId) ||
                (b.BlockerUserId == otherUserId && b.BlockedUserId == userId)));

        public Task<IReadOnlyList<UserBlock>> GetBlockedByUserAsync(Guid blockerUserId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UserBlock>>(Blocks.Where(b => b.BlockerUserId == blockerUserId).ToList());

        public Task AddAsync(UserBlock block, CancellationToken cancellationToken)
        {
            Blocks.Add(block);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken)
        {
            Blocks.RemoveAll(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
