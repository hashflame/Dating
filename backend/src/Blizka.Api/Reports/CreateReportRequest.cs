using Blizka.App.Domain.Enums;

namespace Blizka.Api.Reports;

/// <summary>Тело запроса жалобы (T-17.1). <c>reason</c>/значения перечислены в спеке S-13.</summary>
public sealed record CreateReportRequest(ReportReason Reason, string? Comment, bool BlockUser);
