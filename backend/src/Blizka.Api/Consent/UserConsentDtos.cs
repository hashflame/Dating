using Blizka.App.Domain.Enums;

namespace Blizka.Api.Consent;

/// <summary>Тело запроса <c>POST /api/users/me/consent</c>.</summary>
/// <param name="Type">Тип согласия.</param>
/// <param name="Version">Версия документа (условий использования/политики конфиденциальности), с которой согласился пользователь.</param>
public sealed record RecordConsentRequest(ConsentType Type, string Version);

/// <summary>Зафиксированное согласие пользователя.</summary>
/// <param name="Type">Тип согласия.</param>
/// <param name="Version">Версия документа, с которой согласился пользователь.</param>
/// <param name="Timestamp">Момент фиксации согласия на сервере.</param>
public sealed record UserConsentResponse(ConsentType Type, string Version, DateTimeOffset Timestamp);
