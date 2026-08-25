import i18next, { type i18n } from 'i18next'
import { initReactI18next } from 'react-i18next'

import { DEFAULT_NAMESPACE, FALLBACK_LOCALE, resources, toSupportedLocale } from './config'

/**
 * Держит `<html lang>` в соответствии с языком интерфейса.
 *
 * В `index.html` атрибут задан статически (`ru`), а язык приходит от Telegram
 * и меняется переключателем в панели разработки. Без синхронизации у
 * англоязычного пользователя браузер и скринридер считали бы страницу русской.
 */
function syncDocumentLanguage(instance: i18n): void {
  const apply = (language: string): void => {
    document.documentElement.lang = language
  }

  apply(instance.language)
  instance.on('languageChanged', apply)
}

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

  syncDocumentLanguage(i18next)

  return i18next
}
