using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blizka.Api;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Geo;
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
using NetTopologySuite.Geometries;

namespace Blizka.IntegrationTests.Controllers;

/// <summary>Проверяет <see cref="Blizka.Api.Controllers.GeoController"/> (T-4.1) по тому же минимальному тестовому хосту, что и <see cref="PhotosControllerTests"/>.</summary>
public sealed class GeoControllerTests : IAsyncLifetime
{
    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeCityRepository _cityRepository = null!;
    private FakeNominatimGeocoder _geocoder = null!;

    public async Task InitializeAsync()
    {
        _cityRepository = new FakeCityRepository();
        _geocoder = new FakeNominatimGeocoder();

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
                    services.AddSingleton<ICityRepository>(_cityRepository);
                    services.AddSingleton<INominatimGeocoder>(_geocoder);
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА определение города отклоняется с 401")]
    public async Task Detect_without_token_returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/geo/detect")
        {
            Content = JsonContent.Create(new { lat = 53.9, lon = 27.55 }),
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА рядом есть город каталога ТОГДА ответ 200 содержит его и адрес от геокодера")]
    public async Task Detect_returns_the_nearest_city_and_the_geocoded_address()
    {
        _cityRepository.NearestResult = new City
        {
            Id = Guid.NewGuid(),
            NameRu = "Минск",
            NameBe = "Мінск",
            NameEn = "Minsk",
            Country = "BY",
            Coordinates = new Point(0, 0),
            IsOpen = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _geocoder.Address = "Мінск, Беларусь";
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/geo/detect?locale=ru")
        {
            Content = JsonContent.Create(new { lat = 53.9, lon = 27.55 }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<GeoDetectResponse>>();
        Assert.Equal("Минск", body!.Data.City!.Name);
        Assert.Equal("Мінск, Беларусь", body.Data.DetectedAddress);
    }

    [Fact(DisplayName = "КОГДА широта вне диапазона [-90, 90] ТОГДА ответ 400 VALIDATION_ERROR")]
    public async Task Detect_with_an_out_of_range_latitude_returns_400_validation_error()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/geo/detect")
        {
            Content = JsonContent.Create(new { lat = 999, lon = 27.55 }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
    }

    private string IssueToken()
    {
        var jwtTokenService = _host.Services.GetRequiredService<IJwtTokenService>();
        var user = new User { Id = Guid.NewGuid(), TelegramId = 1, Locale = "ru", Status = UserStatus.Active };
        return jwtTokenService.IssueToken(user).Token;
    }

    private sealed class FakeCityRepository : ICityRepository
    {
        public City? NearestResult { get; set; }

        public Task<bool> ExistsAsync(Guid cityId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<City>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<City?> FindNearestAsync(Point location, double maxDistanceMeters, CancellationToken cancellationToken) =>
            Task.FromResult(NearestResult);
    }

    private sealed class FakeNominatimGeocoder : INominatimGeocoder
    {
        public string? Address { get; set; }

        public Task<string?> ReverseGeocodeAsync(double lat, double lon, CityLocale locale, CancellationToken cancellationToken) =>
            Task.FromResult(Address);
    }
}
