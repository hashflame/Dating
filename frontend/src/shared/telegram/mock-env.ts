import { emitEvent, mockTelegramEnv } from '@tma.js/sdk-react'

import { getMockThemeParams, watchSystemColorScheme } from './theme-mock'

const NO_INSETS = { top: 0, bottom: 0, left: 0, right: 0 }

function emitViewport(): void {
  emitEvent('viewport_changed', {
    height: window.innerHeight,
    width: window.innerWidth,
    is_expanded: true,
    is_state_stable: true,
  })
}

const DEV_USER_KEY = 'blizka:dev-user'
const DEFAULT_DEV_USER = { id: 99281932, firstName: 'Дзмітры' }

/** Кто мы для бэкенда: он опознаёт людей по Telegram-id. */
export type DevUser = {
  id: number
  firstName: string
}

/**
 * Пользователь, от имени которого работаем в браузере.
 *
 * Значение задаётся в панели разработки и переживает перезагрузку: подставь
 * свой Telegram-id — и на стенде это будет твой настоящий аккаунт, тот же,
 * что и при запуске внутри Telegram. Ничего в репозитории не хранится.
 */
export function getDevUser(): DevUser {
  try {
    const stored: unknown = JSON.parse(localStorage.getItem(DEV_USER_KEY) ?? 'null')
    if (stored === null || typeof stored !== 'object') return DEFAULT_DEV_USER

    const { id, firstName } = stored as Partial<DevUser>

    return Number.isSafeInteger(id) && id !== undefined && id > 0
      ? { id, firstName: firstName?.trim() || DEFAULT_DEV_USER.firstName }
      : DEFAULT_DEV_USER
  } catch {
    return DEFAULT_DEV_USER
  }
}

export function setDevUser(user: DevUser): void {
  localStorage.setItem(DEV_USER_KEY, JSON.stringify(user))
}

/**
 * Собирается функцией, а не на верхнем уровне модуля: иначе сборщик считает
 * модуль побочно-эффектным и не вырезает его из production-сборки.
 */
function createMockInitData(): string {
  return new URLSearchParams([
    [
      'user',
      JSON.stringify({
        id: getDevUser().id,
        first_name: getDevUser().firstName,
        language_code: 'ru',
        // Аватар не подставляем: картинки на Telegram CDN у нас нет, и импорт
        // фото из Telegram неизбежно падал бы. Без него кнопка просто не
        // показывается, а в настоящем клиенте аватар приходит настоящий.
      }),
    ],
    ['auth_date', '1716922846'],
    ['signature', 'mock-signature'],
    ['hash', 'mock-hash'],
  ]).toString()
}

/**
 * Подменяет окружение Telegram, чтобы приложение открывалось в браузере.
 * Вне Telegram никто не отвечает на запросы SDK, поэтому отвечаем сами: без этого
 * монтирование темы и вьюпорта зависает или падает с UnknownEnvError.
 * initData фальшивый — реальное API его не примет.
 */
export function mockTelegramEnvironment(): void {
  mockTelegramEnv({
    launchParams: {
      tgWebAppData: createMockInitData(),
      tgWebAppVersion: '8.0',
      tgWebAppPlatform: 'tdesktop',
      tgWebAppThemeParams: getMockThemeParams(),
    },
    onEvent: (event) => {
      switch (event.name) {
        case 'web_app_request_theme':
          return emitEvent('theme_changed', { theme_params: getMockThemeParams() })

        case 'web_app_request_viewport':
          return emitViewport()

        case 'web_app_request_safe_area':
          return emitEvent('safe_area_changed', NO_INSETS)

        case 'web_app_request_content_safe_area':
          return emitEvent('content_safe_area_changed', NO_INSETS)

        default:
          return
      }
    },
  })

  window.addEventListener('resize', emitViewport)
  watchSystemColorScheme()
}
