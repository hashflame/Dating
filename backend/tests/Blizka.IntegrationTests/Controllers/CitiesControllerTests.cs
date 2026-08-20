using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blizka.Api;
using Blizka.Api.Cities;
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
using NetTopologySuite.Geometries;

namespace Blizka.IntegrationTests.Controllers;

/// <summary>Проверяет <see cref="Blizka.Api.Controllers.CitiesController"/> (T-4.1) по тому же минимальному тестовому хосту, что и <see cref="PhotosControllerTests"/>.</summary>
public sealed class CitiesControllerTests : IAsyncLifetime
{
    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeCityRepository _cityRepository = null!;

    public async Task InitializeAsync()
    {
        _cityRepository = new FakeCityRepository();

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
                    services.AddSingleton<ICityRepository>(_cityRepository);
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА поиск городов отклоняется с 401")]
    public async Task Search_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/cities/search?q=Минск");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА найдены города ТОГДА ответ 200 содержит их в порядке, отданном репозиторием")]
    public async Task Search_returns_the_cities_from_the_repository()
    {
        _cityRepository.SearchResult = [CreateCity("Минск", "Мінск", "Minsk", "BY")];
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/cities/search?q=Минск&locale=ru");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CityDto[]>>();
        var city = Assert.Single(body!.Data);
        Assert.Equal("Минск", city.Name);
        Assert.Equal("BY", city.Country);
    }

    [Fact(DisplayName = "КОГДА q не передан ТОГДА ответ 400 VALIDATION_ERROR и репозиторий не вызывается")]
    public async Task Search_without_q_returns_400_validation_error()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/cities/search");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
        Assert.False(_cityRepository.WasSearchCalled);
    }

    private static City CreateCity(string nameRu, string nameBe, string nameEn, string country) => new()
    {
        Id = Guid.NewGuid(),
        NameRu = nameRu,
        NameBe = nameBe,
        NameEn = nameEn,
        Country = country,
        Coordinates = new Point(0, 0),
        IsOpen = true,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private string IssueToken()
    {
        var jwtTokenService = _host.Services.GetRequiredService<IJwtTokenService>();
        var user = new User { Id = Guid.NewGuid(), TelegramId = 1, Locale = "ru", Status = UserStatus.Active };
        return jwtTokenService.IssueToken(user).Token;
    }

    private sealed class FakeCityRepository : ICityRepository
    {
        public IReadOnlyList<City> SearchResult { get; set; } = [];

        public bool WasSearchCalled { get; private set; }

        public Task<bool> ExistsAsync(Guid cityId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<City>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken)
        {
            WasSearchCalled = true;
            return Task.FromResult(SearchResult);
        }

        public Task<City?> FindNearestAsync(Point location, double maxDistanceMeters, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
