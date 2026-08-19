import i18next, { type i18n } from 'i18next'
import { initReactI18next } from 'react-i18next'

import { DEFAULT_NAMESPACE, FALLBACK_LOCALE, resources, toSupportedLocale } from './config'

/** Создаёт и инициализирует инстанс i18next. Вызывается один раз при старте. */
export async function initI18n(language: string | undefined): Promise<i18n> {
  await i18next.use(initReactI18next).init({
    resources,
    lng: toSupportedLocale(language),
    fallbackLng: FALLBACK_LOCALE,
    defaultNS: DEFAULT_NAMESPACE,
    ns: [DEFAULT_NAMESPACE],
    interpolation: { escapeValue: false },
    returnNull: false,
  })

  return i18next
}
