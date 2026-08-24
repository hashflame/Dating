/**
 * Сверено с backend: `Blizka.App/Domain/Enums/UserStatus.cs`.
 *
 * Порядок жизненного цикла: `new` → `onboarding` (ставится при сохранении
 * первого шага черновика) → `active` (после `POST /api/onboarding/complete`).
 * `banned` и `deleted` до фронта не доходят — на них бэкенд отвечает ошибкой.
 *
 * Внимание: в ответе входа это поле объявлено как `string`, а не как энум,
 * поэтому приходит в PascalCase («New»). Приводим к этому виду в `use-session`.
 */
export const USER_STATUSES = [
  'new',
  'onboarding',
  'active',
  'paused',
  'shadowbanned',
  'banned',
  'deleted',
] as const

export type UserStatus = (typeof USER_STATUSES)[number]

/** Сверено с backend: `Blizka.Api/Auth/AuthTelegramResponse.cs`. */
export type Session = {
  /** JWT для заголовка `Authorization: Bearer`. */
  token: string
  /** ISO-8601, UTC. */
  expiresAt: string
  userId: string
  status: UserStatus
  isNewUser: boolean
  /** Язык из профиля Telegram, который бэкенд запомнил при первом входе. */
  locale: string
}
