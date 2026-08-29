import { type DatePreferenceCode } from '../types/date-preference'

const STORAGE_KEY_PREFIX = 'blizka:date-preferences:'

const CODES: readonly DatePreferenceCode[] = [
  'activeOutdoors',
  'calmHangout',
  'quizzesBoardGames',
  'somethingNew',
]

/**
 * Что человек в последний раз сохранил — зеркало на устройстве.
 *
 * Прочитать предпочтения с сервера нечем: `GET /api/users/me/date-preferences`
 * отдаёт 405, и в `GET /api/users/me` их тоже нет (см. docs/api-gaps.md).
 * Без зеркала форма правки анкеты открывалась с пустым выбором, хотя выбор
 * сохранён, — и сохранение затирало его.
 *
 * Ключ с id пользователя: в панели разработки под одним браузером входят
 * разными аккаунтами, и общий ключ показал бы чужой выбор.
 *
 * Уедет вместе с появлением эндпоинта: зеркало знает только про то устройство,
 * где сохраняли, — на другом телефоне выбор снова будет неизвестен.
 */
export function getSavedDatePreferences(userId: string): DatePreferenceCode[] | null {
  const raw = localStorage.getItem(STORAGE_KEY_PREFIX + userId)
  if (raw === null) return null

  return raw === '' ? [] : raw.split(',').filter(isDatePreferenceCode)
}

export function setSavedDatePreferences(userId: string, codes: DatePreferenceCode[]): void {
  localStorage.setItem(STORAGE_KEY_PREFIX + userId, codes.join(','))
}

function isDatePreferenceCode(value: string): value is DatePreferenceCode {
  return CODES.some((code) => code === value)
}
