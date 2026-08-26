using System.Threading.RateLimiting;
using Amazon.S3;
using Amazon.Runtime;
using Blizka.App.Domain.Repositories;
using Blizka.App.Domain.Services;
using Blizka.Data.DevSeed;
using Blizka.Data.Geo;
using Blizka.Data.Http;
using Blizka.Data.Repositories;
using Blizka.Data.Storage;
using Blizka.Data.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Blizka.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddDataLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["Database:ConnectionString"]
            ?? throw new InvalidOperationException("Database:ConnectionString is not configured.");

        services.AddDbContext<BlizkaDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite()));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOnboardingDraftRepository, OnboardingDraftRepository>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IInterestRepository, InterestRepository>();
        services.AddScoped<IUserConsentRepository, UserConsentRepository>();
        services.AddScoped<IPrivacySettingsRepository, PrivacySettingsRepository>();
        services.AddScoped<IUserBlockRepository, UserBlockRepository>();
        services.AddScoped<IUserDatePreferenceRepository, UserDatePreferenceRepository>();
        services.AddScoped<ISparkTransactionRepository, SparkTransactionRepository>();
        services.AddScoped<IPhotoRepository, PhotoRepository>();
        services.AddScoped<IFeedRepository, FeedRepository>();
        services.AddScoped<IUserFilterRepository, UserFilterRepository>();
        services.AddScoped<ISwipeRepository, SwipeRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<ILikesRepository, LikesRepository>();
        services.AddScoped<IQuestionOfDayRepository, QuestionOfDayRepository>();
        services.AddScoped<IQuestionAnswerRepository, QuestionAnswerRepository>();
        services.AddScoped<IDemoSeedService, DemoSeedService>();

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint), "Storage:Endpoint не задан.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Bucket), "Storage:Bucket не задан.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.PublicBaseUrl), "Storage:PublicBaseUrl не задан.")
            // ForcePathStyle=true (ниже) сам добавляет /{Bucket} к ServiceURL при каждом запросе к S3 —
            // если Endpoint уже оканчивается на /{Bucket}, объект физически ложится под задвоенным
            // префиксом (bucket=Bucket, key=Bucket/photos/...), и все публичные ссылки на фото превращаются
            // в 404. Ловим это на старте, а не после того, как в бакет улетит очередная порция объектов.
            .Validate(
                o => string.IsNullOrWhiteSpace(o.Endpoint) || string.IsNullOrWhiteSpace(o.Bucket)
                    || !o.Endpoint.TrimEnd('/').EndsWith($"/{o.Bucket}", StringComparison.OrdinalIgnoreCase),
                "Storage:Endpoint не должен включать в себя Storage:Bucket как сегмент пути — это S3 API endpoint (например, http://minio.internal:9000), а не публичный URL. Имя бакета уже подставляется отдельно (ForcePathStyle).")
            .ValidateOnStart();
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var storageOptions = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            var s3Config = new AmazonS3Config
            {
                ServiceURL = storageOptions.Endpoint,
                ForcePathStyle = true,
            };
            return new AmazonS3Client(new BasicAWSCredentials(storageOptions.AccessKey, storageOptions.SecretKey), s3Config);
        });
        services.AddScoped<IPhotoStorageService, S3PhotoStorageService>();

        services.AddHttpClient<ITelegramAvatarDownloader, TelegramAvatarDownloader>(client =>
                client.Timeout = TimeSpan.FromSeconds(10))
            // ImportTelegramPhotoCommandValidator допускает только photoUrl с хостом t.me (защита от SSRF) —
            // если бы клиент следовал редиректам автоматически, ответ с Location на произвольный хост обошёл
            // бы эту проверку незаметно для валидатора, который видит только исходный URL.
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });

        services.AddOptions<GeoOptions>()
            .Bind(configuration.GetSection(GeoOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.NominatimUserAgent), "Geo:NominatimUserAgent не задан.")
            .ValidateOnStart();
        services.AddHttpClient<INominatimGeocoder, NominatimGeocoder>((sp, client) =>
        {
            var geoOptions = sp.GetRequiredService<IOptions<GeoOptions>>().Value;
            client.BaseAddress = new Uri(geoOptions.NominatimBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
            // Обязательное требование usage policy публичного Nominatim — без опознаваемого User-Agent сервис
            // блокирует запросы по IP.
            client.DefaultRequestHeaders.UserAgent.ParseAdd(geoOptions.NominatimUserAgent);
        });
        // Общий на все запросы к Nominatim лимитер — usage policy публичного инстанса разрешает 1 запрос/сек
        // с одного IP, а источник у всех запросов бэкенда один. Разрешаем небольшую очередь на всплеск
        // (несколько параллельных /api/geo/detect), но не бесконечно — то, что не влезло, просто не обогащается
        // адресом (см. NominatimGeocoder), вместо риска забанить по IP весь бэкенд разом.
        services.AddSingleton<RateLimiter>(_ => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 3,
        }));

        services.AddOptions<TelegramOptions>()
            .Bind(configuration.GetSection(TelegramOptions.SectionName))
            .ValidateOnStart();
        services.AddHttpClient<ITelegramBotService, TelegramBotService>((sp, client) =>
            {
                var telegramOptions = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
                client.BaseAddress = new Uri($"https://api.telegram.org/bot{telegramOptions.BotToken}/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            // BotToken — часть BaseAddress (так требует Bot API: /bot{token}/{method}), поэтому он попадает
            // в каждый относительный URL запроса. Штатные логирующие хендлеры Microsoft.Extensions.Http
            // пишут полный URI на уровне Information при каждом вызове — без RemoveAllLoggers токен бота
            // (полный контроль над ботом: рассылка сообщений, создание invoice-ссылок) утёк бы в логи.
            .RemoveAllLoggers();
        // Отдельный от Nominatim (см. выше) keyed-лимитер — Telegram допускает ~30 запросов/сек в бота
        // (decomposition.md, T-10.1), а не 1/сек. Keyed, а не ещё один AddSingleton<RateLimiter>, чтобы
        // не перезаписать регистрацию Nominatim (при разрешении незакеенного RateLimiter DI отдаёт только
        // последнюю зарегистрированную реализацию).
        services.AddKeyedSingleton<RateLimiter>("telegram", (_, _) => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromSeconds(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 100,
        }));

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgres");

        return services;
    }
}
