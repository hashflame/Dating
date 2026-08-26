namespace Blizka.App.UseCases.Privacy;

/// <summary>Настройки приватности пользователя — общая проекция для GET и PATCH (T-16.1).</summary>
public sealed record PrivacySettingsResult(
    bool BlockIncomingMessages,
    bool HideDistance,
    bool HideAge,
    bool ShowLastActive,
    bool InvisibleMode);
