using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Blizka.Data;

/// <summary>
/// Design-time фабрика, чтобы `dotnet ef migrations add` работал без поднятия полного DI-контейнера Host.
/// Использует connection string для локальной разработки; в рантайме конфигурация всегда берётся из appsettings Blizka.Host.
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
