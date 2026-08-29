import { SUPPORTED_LOCALES, type Locale } from './config'

const STORAGE_KEY = 'blizka:locale'

/**
 * Язык, выбранный человеком вручную. `null` — не выбирал, и тогда интерфейс
 * идёт за настройками Telegram.
 *
 * Хранится на устройстве, а не на сервере: сохранить локаль в API нечем —
 * `GET /api/users/me` её отдаёт, но менять её нельзя (см. docs/api-gaps.md).
 * На каждый запрос `apiRequest` шлёт `?locale=`, поэтому серверные тексты
 * приходят на выбранном языке и без сохранения на бэкенде.
 */
export function getStoredLocale(): Locale | null {
  const stored = localStorage.getItem(STORAGE_KEY)

  return SUPPORTED_LOCALES.find((locale) => locale === stored) ?? null
}

export function setStoredLocale(locale: Locale): void {
  localStorage.setItem(STORAGE_KEY, locale)
}
