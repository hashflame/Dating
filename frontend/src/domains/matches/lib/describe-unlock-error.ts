import { isApiError } from '@/shared/api'

/**
 * Причина отказа `unlock` (S-32/S-33): `402` — не хватает зорок, текст держим
 * в i18n, чтобы звать людей пополнить баланс единообразно с остальным
 * приложением. `422 CONTACT_UNLOCK_UNAVAILABLE` — у собеседника нет
 * публичного username в Telegram, зорки не списаны (T-7.3): сервер уже
 * объясняет это конкретнее, чем любой общий текст, поэтому показываем
 * `error.message` как есть, а не свой шаблон.
 */
export function describeUnlockError(reason: unknown, fallback: string, noSparks: string): string {
  if (!isApiError(reason)) return fallback
  if (reason.status === 402) return noSparks
  if (reason.code === 'CONTACT_UNLOCK_UNAVAILABLE') return reason.message

  return fallback
}
