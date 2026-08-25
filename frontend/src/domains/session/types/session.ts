/**
 * Сверено с backend: `Blizka.App/Domain/Enums/UserStatus.cs`.
 *
 * Порядок жизненного цикла: `new` → `onboarding` (ставится при сохранении
 * первого шага черновика) → `active` (после `POST /api/onboarding/complete`).
 * `banned` и `deleted` до фронта не доходят — на них бэкенд отвечает ошибкой.
 */
export type UserStatus =
  'new' | 'onboarding' | 'active' | 'paused' | 'shadowbanned' | 'banned' | 'deleted'

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
