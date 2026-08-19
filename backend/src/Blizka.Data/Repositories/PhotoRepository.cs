using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Blizka.Data.Repositories;

public sealed class PhotoRepository(BlizkaDbContext dbContext) : IPhotoRepository
{
    private static readonly string[] PhotoRaceConstraintNames =
        ["IX_Photos_UserId_SortOrder", "IX_Photos_UserId_IsMain"];

    public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Photos.CountAsync(p => p.UserId == userId, cancellationToken);

    public Task<List<Photo>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Photos
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Photo photo, CancellationToken cancellationToken) =>
        await dbContext.Photos.AddAsync(photo, cancellationToken);

    public void Remove(Photo photo) => dbContext.Photos.Remove(photo);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsPhotoRaceViolation(ex))
        {
            var conflictingPhoto = dbContext.ChangeTracker.Entries<Photo>()
                .Select(entry => entry.Entity)
                .FirstOrDefault(photo => dbContext.Entry(photo).State == EntityState.Added);

            throw new ConcurrentPhotoUploadException(conflictingPhoto?.UserId ?? Guid.Empty, ex);
        }
    }

    private static bool IsPhotoRaceViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException &&
        PhotoRaceConstraintNames.Contains(postgresException.ConstraintName);
}
