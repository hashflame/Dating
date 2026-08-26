using Blizka.Api;
using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.App;
using Blizka.Data;
using Blizka.Host.BackgroundServices;
using Blizka.Host.Jobs;
using Blizka.Host.OpenApi;
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

// AddOpenApi() строит схему через System.Text.Json.JsonSchemaExporter поверх Microsoft.AspNetCore.Http.Json.JsonOptions —
// это ОТДЕЛЬНЫЙ набор JsonSerializerOptions от Microsoft.AspNetCore.Mvc.JsonOptions, который настраивается
// в AddApiLayer() (Blizka.Api). NumberHandling.Strict нужно продублировать здесь же, иначе фактический рантайм
// парсит числа строго, а сгенерированная спека всё равно описывает их как type: ["integer","string"]
// (унаследовано от JsonSerializerDefaults.Web, см. комментарий рядом с AddJsonOptions в ApiServiceCollectionExtensions).
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
    options.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Blizka API";
        document.Info.Description = "Бэкенд для Telegram Mini App дейтинг-продукта Блізка.";
        return Task.CompletedTask;
    });
    options.AddSchemaTransformer<OnboardingDraftDataSchemaTransformer>();
});

builder.Services.AddApiLayer(builder.Configuration);
builder.Services.AddAppLayer(builder.Configuration);
builder.Services.AddDataLayer(builder.Configuration);

builder.Services.AddQuartz(q =>
{
    // T-7.4 — первая реальная джоба в проекте, список пуст с момента, когда Quartz был подключен под T-0.1.
    var archiveStaleMatchesJobKey = new JobKey(nameof(ArchiveStaleMatchesJob));
    q.AddJob<ArchiveStaleMatchesJob>(options => options.WithIdentity(archiveStaleMatchesJobKey));
    q.AddTrigger(options => options
        .ForJob(archiveStaleMatchesJobKey)
        .WithIdentity($"{nameof(ArchiveStaleMatchesJob)}-trigger")
        .WithSimpleSchedule(schedule => schedule.WithIntervalInHours(6).RepeatForever()));

    // T-11.1 — выбирает вопрос дня раз в сутки в 18:50 по UTC (decomposition.md: 18:50/19:00 — время без
    // указания часового пояса нигде в проекте не привязано к конкретному городу/локали, весь бэкенд и так
    // работает в UTC, см. DateTimeOffset.UtcNow по всему коду). Сама публикация (PublishedAt) откладывается
    // джобой до 19:00 того же дня — см. GenerateQuestionOfDayJob.
    var generateQuestionOfDayJobKey = new JobKey(nameof(GenerateQuestionOfDayJob));
    q.AddJob<GenerateQuestionOfDayJob>(options => options.WithIdentity(generateQuestionOfDayJobKey));
    q.AddTrigger(options => options
        .ForJob(generateQuestionOfDayJobKey)
        .WithIdentity($"{nameof(GenerateQuestionOfDayJob)}-trigger")
        .WithCronSchedule("0 50 18 * * ?", cron => cron.InTimeZone(TimeZoneInfo.Utc)));

    // T-17.1 — раз в 2 часа проверяет накопление жалоб (3+ за 24 часа) и ставит shadowban.
    var shadowbanAutoCheckJobKey = new JobKey(nameof(ShadowbanAutoCheckJob));
    q.AddJob<ShadowbanAutoCheckJob>(options => options.WithIdentity(shadowbanAutoCheckJobKey));
    q.AddTrigger(options => options
        .ForJob(shadowbanAutoCheckJobKey)
        .WithIdentity($"{nameof(ShadowbanAutoCheckJob)}-trigger")
        .WithSimpleSchedule(schedule => schedule.WithIntervalInHours(2).RepeatForever()));
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

// T-10.2 — читает INotificationQueue (зарегистрирована в AddAppLayer) и шлёт сообщения через Telegram.
builder.Services.AddHostedService<NotificationDispatchBackgroundService>();

// T-16.2 — читает IDataExportQueue (зарегистрирована в AddAppLayer), собирает JSON-архив данных
// пользователя и ставит Telegram-уведомление со ссылкой в ту же очередь, что и NotificationDispatchBackgroundService.
builder.Services.AddHostedService<DataExportDispatchBackgroundService>();

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
