using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Entities;

public sealed class Report
{
    public Guid Id { get; set; }

    public Guid ReporterUserId { get; set; }

    public User? ReporterUser { get; set; }

    public Guid ReportedUserId { get; set; }

    public User? ReportedUser { get; set; }

    public ReportReason Reason { get; set; }

    public string? Comment { get; set; }

    public ReportPriority Priority { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; }
}
