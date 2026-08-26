namespace Blizka.App.DataExport;

/// <summary>Полный снимок данных пользователя для самостоятельной выгрузки (T-16.2, GDPR-style data export).</summary>
public sealed record DataExportPayload(
    DataExportProfile Profile,
    IReadOnlyList<DataExportPhoto> Photos,
    IReadOnlyList<string> Interests,
    IReadOnlyList<DataExportConsent> Consents,
    DataExportPrivacySettings? PrivacySettings,
    DateTimeOffset GeneratedAt);

public sealed record DataExportProfile(
    Guid UserId,
    long TelegramId,
    string? TelegramUsername,
    string Name,
    DateOnly BirthDate,
    string Gender,
    string? CityName,
    string? Bio,
    int? Height,
    string? Smoking,
    string? Drinking,
    string? Chronotype,
    bool? HasChildren,
    string[] Prompts,
    string? InstagramHandle,
    bool IsVerified,
    int SparksBalance,
    int ProfileCompleteness,
    string Status,
    string Locale,
    DateTimeOffset CreatedAt);

public sealed record DataExportPhoto(string Url, int SortOrder, bool IsMain, DateTimeOffset CreatedAt);

public sealed record DataExportConsent(string Type, string Version, DateTimeOffset Timestamp, bool AgeConfirmed);

public sealed record DataExportPrivacySettings(
    bool BlockIncomingMessages, bool HideDistance, bool HideAge, bool ShowLastActive, bool InvisibleMode);
