import be from './locales/be/common.json'
import en from './locales/en/common.json'
import ru from './locales/ru/common.json'

export const SUPPORTED_LOCALES = ['ru', 'be', 'en'] as const

export type Locale = (typeof SUPPORTED_LOCALES)[number]

export const FALLBACK_LOCALE: Locale = 'ru'

export const DEFAULT_NAMESPACE = 'common'

export const resources = {
  ru: { common: ru },
  be: { common: be },
  en: { common: en },
} as const

export function toSupportedLocale(value: string | undefined): Locale {
  const short = value?.slice(0, 2).toLowerCase()
  return SUPPORTED_LOCALES.find((locale) => locale === short) ?? FALLBACK_LOCALE
}
