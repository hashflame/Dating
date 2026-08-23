using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Consent;

/// <summary>Статус согласия пользователя по одному <see cref="ConsentType"/> — <c>Given=false</c>, если записи ещё нет.</summary>
public sealed record UserConsentStatusResult(ConsentType Type, bool Given, string? Version, DateTimeOffset? Timestamp);
