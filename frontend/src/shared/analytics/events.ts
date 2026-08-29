import { type ROUTES } from '@/shared/config'

/**
 * Словарь событий: имя и его свойства в одном объекте. Свободных строк на
 * вызове нет — опечатка в имени или лишнее свойство не проходят компиляцию.
 *
 * Что сюда попадает: перечисления, числа и флаги. Имена, фото, тексты
 * сообщений, `telegramId` и id мэтчей — никогда. Разбирать людей поимённо
 * незачем, а утечь такое может ровно один раз.
 *
 * Событие заводится вместе с местом вызова: «на будущее» здесь ничего нет.
 */

/** Стабильное имя экрана — ключ `ROUTES`, а не путь: в путях есть id мэтчей. */
export type ScreenName = keyof typeof ROUTES

/** Шаги анкеты. Фото и интересы сохраняются своими доменами, но шаг — общий. */
export type OnboardingStepName = 'about' | 'preferences' | 'city' | 'photos' | 'interests'

export type AnalyticsEvent =
  /** Запуск приложения. Из него PostHog сам считает DAU/MAU и retention. */
  | { name: 'app_opened'; start_param: string | null; platform: string }
  | { name: 'screen_viewed'; screen: ScreenName }
  | { name: 'consent_accepted'; consent_version: string }
  | { name: 'onboarding_step_completed'; step: OnboardingStepName; seconds_on_step: number }
  | { name: 'onboarding_completed'; profile_completeness: number }
  | { name: 'photo_uploaded'; source: 'file' | 'telegram'; index: number }
  /** `reason` — код ошибки из `ApiError`, не текст: тексты меняются, коды нет. */
  | { name: 'photo_upload_failed'; source: 'file' | 'telegram'; reason: string }
  /**
   * Решение по анкете. `source` отделяет ленту от ответа на входящую симпатию:
   * это одно и то же действие на сервере, но разные места в продукте, и
   * смешивать их в воронке ленты нельзя.
   *
   * `position` — какой по счёту свайп в ленте за сессию: видно, где люди
   * перестают листать. `seconds_on_card` вне ленты не измеряется — там `null`.
   */
  | {
      name: 'swipe'
      source: 'feed' | 'likes' | 'matches'
      action: 'like' | 'dislike'
      position: number
      seconds_on_card: number | null
      is_match: boolean
    }
  | { name: 'swipe_undone' }
  /** Карточки кончились — для дейтинга ключевая метрика: такой человек не вернётся. */
  | { name: 'feed_exhausted'; swipes_in_session: number }
  | { name: 'feed_filters_changed'; age_min: number; age_max: number; distance: number }
  /** `cost` — сколько зорок списали; 0 при повторном раскрытии. */
  | { name: 'likes_revealed'; cost: number }
  | { name: 'match_hub_opened' }
  | { name: 'match_archived'; archived: boolean }
  /** `kind` — обычное сообщение мэтчу или суперсообщение из ленты. */
  | { name: 'chat_opened'; kind: 'message' | 'super'; sparks_spent: number }
  /** Лимиты и запреты — узкое место продукта, отказ важнее удачной отправки. */
  | { name: 'message_blocked'; kind: 'message' | 'super'; reason: string }
  | { name: 'user_reported'; reason: string; also_blocked: boolean }
  | { name: 'user_blocked' }
