using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Reports;

/// <summary>Подача жалобы на пользователя (T-17.1) — опционально с одновременной блокировкой.</summary>
public sealed record CreateReportCommand(
    Guid ReporterUserId, Guid ReportedUserId, ReportReason Reason, string? Comment, bool BlockUser) : IRequest;
