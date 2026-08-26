using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Blocks;
using FluentValidation;
using MediatR;

namespace Blizka.App.UseCases.Reports;

/// <summary>
/// Подаёт жалобу на пользователя (T-17.1) и маппит причину в приоритет: <c>Underage</c>/<c>UnsafeMeeting</c> —
/// Critical (немедленный бан репортящегося аккаунта до ручной проверки модератором, T-17.2), <c>Scam</c>/<c>Explicit</c> —
/// High, остальное — Normal. Массовый автоматический shadowban по накоплению жалоб считает отдельная
/// Quartz-джоба ShadowbanAutoCheckJob (Blizka.Host), эта команда — только приём одной жалобы.
/// </summary>
public sealed class CreateReportCommandHandler(
    IUserRepository userRepository,
    IReportRepository reportRepository,
    IMediator mediator,
    IValidator<CreateReportCommand> validator)
    : IRequestHandler<CreateReportCommand>
{
    public async Task Handle(CreateReportCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var reportedUser = await userRepository.GetByIdAsync(request.ReportedUserId, cancellationToken)
            ?? throw new UserProfileNotFoundException(request.ReportedUserId);

        var priority = MapPriority(request.Reason);

        await reportRepository.AddAsync(
            new Report
            {
                Id = Guid.NewGuid(),
                ReporterUserId = request.ReporterUserId,
                ReportedUserId = reportedUser.Id,
                Reason = request.Reason,
                Comment = request.Comment,
                Priority = priority,
                Status = ReportStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            cancellationToken);

        // Critical (underage / unsafe_meeting) блокирует аккаунт немедленно, не дожидаясь Quartz-джобы
        // ShadowbanAutoCheck — до ручной проверки модератором (T-17.2, ещё не реализован) пользователь уже
        // не должен логиниться (см. проверку Status == Banned в AuthenticateTelegramUserCommandHandler).
        if (priority == ReportPriority.Critical
            && reportedUser.Status is not (UserStatus.Banned or UserStatus.Deleted))
        {
            reportedUser.Status = UserStatus.Banned;
            reportedUser.BanReason = $"Автоматическая блокировка по жалобе ({request.Reason}), ожидает ручной проверки.";
            reportedUser.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await reportRepository.SaveChangesAsync(cancellationToken);

        if (request.BlockUser)
        {
            await mediator.Send(new BlockUserCommand(request.ReporterUserId, request.ReportedUserId), cancellationToken);
        }
    }

    private static ReportPriority MapPriority(ReportReason reason) => reason switch
    {
        ReportReason.Underage or ReportReason.UnsafeMeeting => ReportPriority.Critical,
        ReportReason.Scam or ReportReason.Explicit => ReportPriority.High,
        ReportReason.FakePhotos or ReportReason.Insults or ReportReason.Spam => ReportPriority.Normal,
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Неизвестная причина жалобы."),
    };
}
