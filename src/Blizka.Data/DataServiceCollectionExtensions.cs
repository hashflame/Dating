using Blizka.App.Domain.Repositories;
using Blizka.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgres");

        return services;
    }
}
