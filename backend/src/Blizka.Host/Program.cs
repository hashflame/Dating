using Blizka.Api;
using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.App;
using Blizka.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Quartz;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Railway (и большинство PaaS) не позволяют выбрать фиксированный порт — платформа сама назначает его
// через переменную окружения PORT и ждёт, что контейнер слушает именно его. UseUrls() имеет приоритет
// над ASPNETCORE_URLS из конфигурации/Dockerfile, так что при наличии PORT он побеждает; без него
// (например, при локальном запуске контейнера) остаётся дефолт из ASPNETCORE_URLS/appsettings.
var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(railwayPort))
{
    builder.WebHost.UseUrls($"http://+:{railwayPort}");
}

// WebApplication.CreateBuilder уже добавляет AddEnvironmentVariables() в стандартный набор источников.
// AddYamlFile здесь добавляется позже — по правилам IConfiguration побеждает источник, добавленный последним,
// поэтому без явного повторного AddEnvironmentVariables() в конце YAML перебивал бы переменные окружения
// (в проде на Railway секреты/конфигурация задаются именно через них).
builder.Configuration
    .AddYamlFile("appsettings.yaml", optional: false, reloadOnChange: true)
    .AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
const string corsPolicyName = "TelegramMiniApp";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddExceptionHandler<BlizkaExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Blizka API";
        document.Info.Description = "Бэкенд для Telegram Mini App дейтинг-продукта Блізка.";
        return Task.CompletedTask;
    });
});

builder.Services.AddApiLayer(builder.Configuration);
builder.Services.AddAppLayer(builder.Configuration);
builder.Services.AddDataLayer(builder.Configuration);

builder.Services.AddQuartz();
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // За Railway-прокси TLS обрывается на границе, до контейнера трафик идёт по HTTP — без этого
    // UseHttpsRedirection() ниже уйдёт в redirect-loop, т.к. увидит запрос как HTTP.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseExceptionHandler();

// TODO: временно включено в Production по просьбе пользователя, чтобы глянуть Scalar на Railway —
// вернуть проверку `app.Environment.IsDevelopment()` обратно после того, как посмотрит.
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseCors(corsPolicyName);
app.UseMiddleware<TelegramAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteResponse,
});

app.Run();

public partial class Program;
