using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.App.Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Blizka.IntegrationTests.ErrorHandling;

/// <summary>
/// Exercises <see cref="BlizkaExceptionHandler"/> over real HTTP through a minimal test host
/// (rather than <c>WebApplicationFactory&lt;Program&gt;</c>) so it stays independent of unrelated
/// Blizka.Host wiring such as CORS/Telegram config.
/// </summary>
public sealed class BlizkaExceptionHandlerTests : IAsyncLifetime
{
    private IHost _host = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddExceptionHandler<BlizkaExceptionHandler>();
                    services.AddProblemDetails();
                });
                webBuilder.Configure(app =>
                {
                    app.UseExceptionHandler();

                    app.Use(async (context, next) =>
                    {
                        var simulatedLocaleClaim = context.Request.Headers["X-Simulated-Locale-Claim"].ToString();
                        if (!string.IsNullOrEmpty(simulatedLocaleClaim))
                        {
                            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                                [new Claim("locale", simulatedLocaleClaim)]));
                        }

                        await next(context);
                    });

                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/throw/insufficient-sparks", IResult () => throw new InsufficientSparksException(10, 3));
                        endpoints.MapGet("/throw/user-banned", IResult () => throw new UserBannedException(Guid.Empty));
                        endpoints.MapGet("/throw/onboarding-incomplete", IResult () => throw new OnboardingIncompleteException("photos"));
                        endpoints.MapGet("/throw/city-not-open", IResult () => throw new CityNotOpenException(Guid.Empty));
                        endpoints.MapGet("/throw/validation", IResult () => throw new ValidationException(
                        [
                            new ValidationFailure("name", "Name is required"),
                            new ValidationFailure("name", "Name must be at least 2 characters"),
                            new ValidationFailure("birthDate", "You must be at least 18 years old"),
                        ]));
                        endpoints.MapGet("/throw/unknown", IResult () => throw new InvalidOperationException("boom"));
                    });
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

    [Fact]
    public async Task InsufficientSparksException_maps_to_402_with_action_and_details()
    {
        var response = await _client.GetAsync("/throw/insufficient-sparks");

        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("INSUFFICIENT_SPARKS", body!.Error.Code);
        Assert.Equal("TOP_UP_SPARKS", body.Error.Action);
        Assert.NotNull(body.Error.Details);
    }

    [Fact]
    public async Task UserBannedException_maps_to_403()
    {
        var response = await _client.GetAsync("/throw/user-banned");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("USER_BANNED", body!.Error.Code);
    }

    [Fact]
    public async Task OnboardingIncompleteException_maps_to_422()
    {
        var response = await _client.GetAsync("/throw/onboarding-incomplete");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("ONBOARDING_INCOMPLETE", body!.Error.Code);
        Assert.Equal("COMPLETE_ONBOARDING", body.Error.Action);
    }

    [Fact]
    public async Task CityNotOpenException_maps_to_409()
    {
        var response = await _client.GetAsync("/throw/city-not-open");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("CITY_NOT_OPEN", body!.Error.Code);
    }

    [Fact]
    public async Task ValidationException_maps_to_400_with_per_field_details()
    {
        var response = await _client.GetAsync("/throw/validation");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
        Assert.Null(body.Error.Action);
    }

    [Fact]
    public async Task Unknown_exception_maps_to_500_without_leaking_exception_details()
    {
        var response = await _client.GetAsync("/throw/unknown");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("INTERNAL_ERROR", body!.Error.Code);
        Assert.DoesNotContain("boom", body.Error.Message);
    }

    [Theory]
    [InlineData("ru", "Ваш аккаунт заблокирован")]
    [InlineData("be", "Ваш акаўнт заблакаваны")]
    [InlineData("en", "Your account is banned")]
    public async Task Message_is_localized_from_locale_claim(string locale, string expectedSubstring)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/throw/user-banned");
        request.Headers.Add("X-Simulated-Locale-Claim", locale);

        var response = await _client.SendAsync(request);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Contains(expectedSubstring, body!.Error.Message);
    }

    [Fact]
    public async Task Falls_back_to_AcceptLanguage_header_when_no_locale_claim_present()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/throw/city-not-open");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.9");

        var response = await _client.SendAsync(request);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Contains("Your city isn't open yet", body!.Error.Message);
    }

    [Fact]
    public async Task Falls_back_to_Russian_when_locale_is_unrecognized()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/throw/city-not-open");
        request.Headers.Add("Accept-Language", "fr-FR");

        var response = await _client.SendAsync(request);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Contains("Ваш город ещё не открыт", body!.Error.Message);
    }
}
