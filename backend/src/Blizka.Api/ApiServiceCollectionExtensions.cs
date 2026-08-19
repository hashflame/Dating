using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.App.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Blizka.Api;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(AssemblyMarker).Assembly)
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)))
            .ConfigureApiBehaviorOptions(options =>
            {
                // По умолчанию [ApiController] сам отвечает на невалидное тело запроса (например, неизвестное
                // значение enum'а не проходит биндинг ещё до FluentValidation) через ValidationProblemDetails —
                // это ломает контракт ApiErrorResponse, единый для всех остальных ошибок API. Приводим к нему.
                options.InvalidModelStateResponseFactory = context =>
                {
                    var locale = RequestLocaleResolver.Resolve(context.HttpContext);
                    var message = ErrorMessageCatalog.Resolve(ErrorMessageCatalog.ValidationError, locale);
                    var details = context.ModelState
                        .Where(entry => entry.Value?.Errors.Count > 0)
                        .ToDictionary(
                            entry => entry.Key,
                            entry => entry.Value!.Errors
                                .Select(error => string.IsNullOrEmpty(error.ErrorMessage)
                                    ? error.Exception?.Message ?? "Invalid value."
                                    : error.ErrorMessage)
                                .ToArray());

                    return new BadRequestObjectResult(
                        ApiErrorResponse.From(ErrorMessageCatalog.ValidationError, message, details));
                };
            });

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Secret), "Jwt:Secret не задан — задайте его в конфигурации (в проде — через переменную окружения), иначе приложение не сможет проверять и выдавать токены.")
            .ValidateOnStart();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorization();

        return services;
    }
}
