using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class ReportRepository(BlizkaDbContext dbContext) : IReportRepository
{
    public async Task AddAsync(Report report, CancellationToken cancellationToken) =>
        await dbContext.Reports.AddAsync(report, cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetUsersExceedingReportThresholdAsync(
        DateTimeOffset since, int thresholdCount, CancellationToken cancellationToken) =>
        await dbContext.Reports.AsNoTracking()
            .Where(r => r.CreatedAt >= since && r.Status == ReportStatus.Pending)
            .Select(r => new { r.ReportedUserId, r.ReporterUserId })
            .Distinct()
            .GroupBy(r => r.ReportedUserId)
            .Where(g => g.Count() >= thresholdCount)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
