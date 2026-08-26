using MediatR;

namespace Blizka.App.UseCases.Privacy;

/// <summary><c>null</c> в любом поле означает «не менять» — как и в <c>PatchUserProfileCommand</c> (T-9.1).</summary>
public sealed record PatchPrivacySettingsCommand(
    Guid UserId,
    bool? BlockIncomingMessages,
    bool? HideDistance,
    bool? HideAge,
    bool? ShowLastActive,
    bool? InvisibleMode) : IRequest<PrivacySettingsResult>;
