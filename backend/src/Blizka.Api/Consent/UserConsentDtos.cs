using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Consent;

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

/// <summary>Статус согласия пользователя по одному типу — ответ <c>GET /api/users/me/consent</c>.</summary>
/// <param name="Type">Тип согласия.</param>
/// <param name="Given">Дано ли согласие хотя бы раз.</param>
/// <param name="Version">Версия документа последнего согласия; <c>null</c>, если согласия ещё не было.</param>
/// <param name="Timestamp">Момент последнего согласия; <c>null</c>, если согласия ещё не было.</param>
public sealed record UserConsentStatusResponse(ConsentType Type, bool Given, string? Version, DateTimeOffset? Timestamp)
{
    public static UserConsentStatusResponse From(UserConsentStatusResult result) =>
        new(result.Type, result.Given, result.Version, result.Timestamp);
}
