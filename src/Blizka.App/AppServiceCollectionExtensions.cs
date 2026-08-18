using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Blizka.App;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddAppLayer(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

        return services;
    }
}
