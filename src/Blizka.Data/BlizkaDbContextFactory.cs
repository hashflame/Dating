using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Blizka.Data;

/// <summary>
/// Design-time factory so `dotnet ef migrations add` works without spinning up the full Host DI container.
/// Uses a local-dev connection string; runtime configuration always comes from Blizka.Host's appsettings.
/// </summary>
public sealed class BlizkaDbContextFactory : IDesignTimeDbContextFactory<BlizkaDbContext>
{
    public BlizkaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BLIZKA_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=blizka;Username=blizka;Password=blizka";

        var optionsBuilder = new DbContextOptionsBuilder<BlizkaDbContext>()
            .UseNpgsql(connectionString, o => o.UseNetTopologySuite());

        return new BlizkaDbContext(optionsBuilder.Options);
    }
}
