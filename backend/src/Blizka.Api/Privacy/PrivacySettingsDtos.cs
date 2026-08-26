using Blizka.App.UseCases.Privacy;

namespace Blizka.Api.Privacy;

/// <summary>Тело запроса <c>PATCH /api/privacy/settings</c> — не переданное (<c>null</c>) поле не меняется.</summary>
public sealed record PatchPrivacySettingsRequest(
    bool? BlockIncomingMessages,
    bool? HideDistance,
    bool? HideAge,
    bool? ShowLastActive,
    bool? InvisibleMode);

/// <summary>Настройки приватности пользователя — ответ <c>GET</c>/<c>PATCH /api/privacy/settings</c> (T-16.1).</summary>
public sealed record PrivacySettingsResponse(
    bool BlockIncomingMessages,
    bool HideDistance,
    bool HideAge,
    bool ShowLastActive,
    bool InvisibleMode)
{
    public static PrivacySettingsResponse From(PrivacySettingsResult result) =>
        new(result.BlockIncomingMessages, result.HideDistance, result.HideAge, result.ShowLastActive, result.InvisibleMode);
}
