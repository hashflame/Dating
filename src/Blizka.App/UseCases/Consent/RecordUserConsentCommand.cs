using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Consent;

public sealed record RecordUserConsentCommand(Guid UserId, long TelegramId, ConsentType Type, string Version, string? IpAddress)
    : IRequest<UserConsentResult>;

public sealed record UserConsentResult(ConsentType Type, string Version, DateTimeOffset Timestamp);
