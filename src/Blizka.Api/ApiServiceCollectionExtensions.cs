using Microsoft.Extensions.DependencyInjection;

namespace Blizka.Api;

public static class ApiServiceCollectionExtensions
{
    public static IMvcBuilder AddApiLayer(this IServiceCollection services)
    {
        return services.AddControllers()
            .AddApplicationPart(typeof(AssemblyMarker).Assembly);
    }
}
