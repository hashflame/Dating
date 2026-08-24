/**
 * Сверено с backend: `Blizka.App/Domain/Enums/ConsentType.cs`.
 * На экране приветствия один общий чекбокс, поэтому значение пока одно.
 */
type ConsentType = 'termsAndPrivacyPolicy'

/** Сверено с backend: `Blizka.Api/Consent/UserConsentDtos.cs`. */
export type UserConsent = {
  type: ConsentType
  version: string
  timestamp: string
}

/** Ответ `GET /api/users/me/consent` — по одной записи на тип согласия. */
export type UserConsentStatus = {
  type: ConsentType
  /** Согласие давали хотя бы раз. Версию проверяем отдельно. */
  given: boolean
  /** `null`, если согласия ещё не было. */
  version: string | null
  timestamp: string | null
}
