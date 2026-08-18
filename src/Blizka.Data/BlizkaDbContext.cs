using Microsoft.EntityFrameworkCore;

namespace Blizka.Data;

public sealed class BlizkaDbContext(DbContextOptions<BlizkaDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssemblyMarker).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
