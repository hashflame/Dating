using Blizka.App.DataExport;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Users;

public sealed class BuildDataExportQueryHandler(
    IUserRepository userRepository,
    IUserConsentRepository userConsentRepository,
    IPrivacySettingsRepository privacySettingsRepository)
    : IRequestHandler<BuildDataExportQuery, DataExportPayload>
{
    public async Task<DataExportPayload> Handle(BuildDataExportQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdWithProfileDataAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        var consents = await userConsentRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var privacySettings = await privacySettingsRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        var profile = new DataExportProfile(
            user.Id,
            user.TelegramId,
            user.TelegramUsername,
            user.Name,
            user.BirthDate,
            user.Gender.ToString(),
            user.City?.NameRu,
            user.Bio,
            user.Height,
            user.Smoking?.ToString(),
            user.Drinking?.ToString(),
            user.Chronotype?.ToString(),
            user.HasChildren,
            user.Prompts,
            user.InstagramHandle,
            user.IsVerified,
            user.SparksBalance,
            user.ProfileCompleteness,
            user.Status.ToString(),
            user.Locale,
            user.CreatedAt);

        var photos = user.Photos
            .OrderBy(p => p.SortOrder)
            .Select(p => new DataExportPhoto(p.Url, p.SortOrder, p.IsMain, p.CreatedAt))
            .ToList();

        var interests = user.UserInterests
            .Where(ui => ui.Interest is not null)
            .Select(ui => ui.Interest!.NameRu)
            .ToList();

        var consentResults = consents
            .Select(c => new DataExportConsent(c.Type.ToString(), c.Version, c.Timestamp, c.AgeConfirmed))
            .ToList();

        var privacyResult = privacySettings is null
            ? null
            : new DataExportPrivacySettings(
                privacySettings.BlockIncomingMessages,
                privacySettings.HideDistance,
                privacySettings.HideAge,
                privacySettings.ShowLastActive,
                privacySettings.InvisibleMode);

        return new DataExportPayload(profile, photos, interests, consentResults, privacyResult, DateTimeOffset.UtcNow);
    }
}
