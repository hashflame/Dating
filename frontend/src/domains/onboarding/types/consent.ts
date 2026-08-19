/**
 * Сверено с backend: `Blizka.App/Domain/Enums/ConsentType.cs`.
 * На экране приветствия один общий чекбокс, поэтому значение пока одно.
 */
export type ConsentType = 'termsAndPrivacyPolicy'

/** Сверено с backend: `Blizka.Api/Consent/UserConsentDtos.cs`. */
export type UserConsent = {
  type: ConsentType
  version: string
  timestamp: string
}
