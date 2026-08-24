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

const DEV_USER_ID_KEY = 'blizka:dev-user-id'
const DEFAULT_DEV_USER_ID = 99281932

/**
 * Telegram-id выдуманного пользователя. Хранится, чтобы между перезагрузками
 * попадать в тот же аккаунт на бэкенде: он опознаёт людей именно по нему.
 */
function devUserId(): number {
  const stored = Number(localStorage.getItem(DEV_USER_ID_KEY))

  return Number.isSafeInteger(stored) && stored > 0 ? stored : DEFAULT_DEV_USER_ID
}

/**
 * Начинает всё заново: выдаёт новый Telegram-id, поэтому следующий вход создаст
 * на бэкенде чистый аккаунт — со статусом `new`, без согласия, черновика и фото.
 *
 * Так выглядит сброс онбординга без поддержки со стороны API: эндпоинта,
 * который откатывает состояние, нет (см. docs/api-gaps.md). Побочный эффект —
 * прошлый тестовый аккаунт остаётся в базе.
 */
export function resetDevUser(): void {
  // Просто следующий по счёту: так id никогда не повторится и не попадёт
  // случайно в уже использованный аккаунт.
  localStorage.setItem(DEV_USER_ID_KEY, String(devUserId() + 1))
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
        id: devUserId(),
        first_name: 'Дзмітры',
        last_name: '',
        username: 'dev_user',
        language_code: 'ru',
        // Аватар не подставляем: у выдуманного пользователя картинки на
        // Telegram CDN нет, и импорт неизбежно падал бы. Без него кнопка
        // «Взять фото из Telegram» просто не показывается, а в настоящем
        // клиенте аватар приходит настоящий и путь работает.
        is_premium: false,
        allows_write_to_pm: true,
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
