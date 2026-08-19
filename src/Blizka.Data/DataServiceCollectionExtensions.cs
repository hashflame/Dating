using Amazon.S3;
using Amazon.Runtime;
using Blizka.App.Domain.Repositories;
using Blizka.App.Domain.Services;
using Blizka.Data.Http;
using Blizka.Data.Repositories;
using Blizka.Data.Storage;
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
        services.AddScoped<IUserConsentRepository, UserConsentRepository>();
        services.AddScoped<IUserDatePreferenceRepository, UserDatePreferenceRepository>();
        services.AddScoped<ISparkTransactionRepository, SparkTransactionRepository>();
        services.AddScoped<IPhotoRepository, PhotoRepository>();

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint), "Storage:Endpoint не задан.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Bucket), "Storage:Bucket не задан.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.PublicBaseUrl), "Storage:PublicBaseUrl не задан.")
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

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgres");

        return services;
    }
}
