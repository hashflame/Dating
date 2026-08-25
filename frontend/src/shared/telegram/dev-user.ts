import { env } from '@/shared/config'

const DEV_USER_KEY = 'blizka:dev-telegram-id'

/**
 * Раньше в localStorage лежал объект с id, именем и юзернеймом. Ключи чистим,
 * чтобы забытое значение из старой версии панели не увело вход на чужой id.
 */
const LEGACY_KEYS = ['blizka:dev-user', 'blizka:dev-user-id']

/**
 * Кто мы для бэкенда. Он опознаёт людей по Telegram-id, поэтому это не
 * «тестовый пользователь», а настоящий аккаунт: dev-сервер подписывает initData
 * настоящим токеном бота (`vite/dev-telegram-auth.ts`), и на стенде получается
 * тот же человек, что и при запуске из Telegram.
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
const DEMO_FALLBACK_ID = 990000000001

/**
 * Пользователь, от имени которого работаем в браузере: по умолчанию из `.env`,
 * поверх — id, выбранный в панели разработки (он переживает перезагрузку).
 *
 * Имя и юзернейм подставляются только своему аккаунту — тому, чей id стоит в
 * `.env`. При входе под чужим id отправляем **только** id, как это делает
 * настоящий логин Telegram: имени и юзернейма того человека мы не знаем, а
 * прислать свои значило бы затереть ими его профиль на бэкенде.
 */
export function getDevUser(): DevUser {
  const id = storedId() ?? env.devUser?.id ?? (env.devLoginSecret ? DEMO_FALLBACK_ID : null)

  if (id === null) {
    throw new Error(
      'Не задан аккаунт для входа из браузера. Впиши DEV_USER_ID, ' +
        'DEV_USER_NAME и DEV_USER_USERNAME в .env — свой Telegram-id ' +
        'подскажет @userinfobot. Подробнее: docs/real-backend.md',
    )
  }

  const own = env.devUser?.id === id ? env.devUser : null

  return {
    id,
    firstName: own?.firstName.trim() ?? '',
    username: own?.username.trim().replace(/^@/, '') ?? '',
  }
}

/** Запоминает выбранный id до следующего выбора. Применяется перезагрузкой. */
export function setDevUserId(id: number): void {
  localStorage.setItem(DEV_USER_KEY, String(id))
}

function storedId(): number | null {
  for (const key of LEGACY_KEYS) {
    localStorage.removeItem(key)
  }

  const raw = Number(localStorage.getItem(DEV_USER_KEY))

  return Number.isSafeInteger(raw) && raw > 0 ? raw : null
}
