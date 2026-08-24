using Blizka.App.Sparks;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Blizka.App;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddAppLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

        services.AddOptions<SparksOptions>()
            .Bind(configuration.GetSection(SparksOptions.SectionName))
            .Validate(o => o.SuperlikeCost > 0, "Sparks:SuperlikeCost должен быть положительным.")
            .Validate(o => o.LikesRevealCost > 0, "Sparks:LikesRevealCost должен быть положительным.")
            .ValidateOnStart();
        services.AddScoped<ISparksService, SparksService>();

        return services;
    }
}
