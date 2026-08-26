using MediatR;

namespace Blizka.App.UseCases.Privacy;

public sealed record GetPrivacySettingsQuery(Guid UserId) : IRequest<PrivacySettingsResult>;
