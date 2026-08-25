import { env } from '@/shared/config'

const DEV_USER_KEY = 'blizka:dev-user'

/**
 * Кто мы для бэкенда. Он опознаёт людей по Telegram-id, поэтому это не
 * «тестовый пользователь», а настоящий аккаунт: dev-сервер подписывает initData
 * настоящим токеном бота (`vite/dev-telegram-auth.ts`), и на стенде получается
 * тот же человек, что и при запуске из Telegram.
 *
 * `username` не косметика: бэкенд пишет его в `TelegramUsername`
 * (`AuthenticateTelegramUserCommandHandler`) при каждом входе, и без него
 * открытие контакта в мэтче возвращает пустую ссылку.
 */
export type DevUser = {
  id: number
  firstName: string
  /** Без `@`. Пустая строка — нет юзернейма, как у аккаунтов без него. */
  username: string
}

/**
 * Первый из 10 фиксированных демо-аккаунтов (backend: `docs/specs/003-demo-seed-data.md`).
 * Запасной вариант только для задеплоенного dev-стенда (`env.devLoginSecret`) — там
 * `initTelegram()` вызывает `getDevUser()` ещё до первого рендера, и без дефолта
 * пустой браузер (ничего в localStorage) падал бы на самом старте, не дав дойти
 * до панели разработки, где можно выбрать другого демо-пользователя.
 */
const DEMO_FALLBACK_USER: DevUser = {
  id: 990000000001,
  firstName: 'Демо 1',
  username: 'demo_user_1',
}

/**
 * Пользователь, от имени которого работаем в браузере: по умолчанию из `.env`,
 * поверх — выбор в панели разработки (он переживает перезагрузку).
 *
 * Запасного значения нет намеренно (кроме demo-стенда, см. `DEMO_FALLBACK_USER`
 * выше) — выдуманный id молча увёл бы работу на чужой или несуществующий
 * аккаунт, вместо этого падаем с внятным текстом.
 */
export function getDevUser(): DevUser {
  const user = fromStorage() ?? fromEnvironment()
  if (user !== null) return user

  if (env.devLoginSecret) return DEMO_FALLBACK_USER

  throw new Error(
    'Не задан аккаунт для входа из браузера. Впиши DEV_USER_ID, ' +
      'DEV_USER_NAME и DEV_USER_USERNAME в .env — свой Telegram-id ' +
      'подскажет @userinfobot. Подробнее: docs/real-backend.md',
  )
}

export function setDevUser(user: DevUser): void {
  localStorage.setItem(DEV_USER_KEY, JSON.stringify(normalize(user)))
}

function fromEnvironment(): DevUser | null {
  return env.devUser === null ? null : normalize(env.devUser)
}

function fromStorage(): DevUser | null {
  try {
    const raw: unknown = JSON.parse(localStorage.getItem(DEV_USER_KEY) ?? 'null')
    if (raw === null || typeof raw !== 'object') return null

    const { id, firstName, username } = raw as Partial<DevUser>
    if (!Number.isSafeInteger(id) || id === undefined || id <= 0) return null

    return normalize({ id, firstName: firstName ?? '', username: username ?? '' })
  } catch {
    return null
  }
}

/** Люди пишут юзернейм с «@» и лишними пробелами, Telegram присылает без. */
function normalize(user: DevUser): DevUser {
  return {
    id: user.id,
    firstName: user.firstName.trim(),
    username: user.username.trim().replace(/^@/, ''),
  }
}
