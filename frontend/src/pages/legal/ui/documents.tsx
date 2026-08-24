import { LegalPage } from './LegalPage'

/** Правила сервиса. */
export function TermsPage() {
  return <LegalPage document="terms" />
}

/** Политика обработки персональных данных. */
export function PrivacyPage() {
  return <LegalPage document="privacy" />
}
