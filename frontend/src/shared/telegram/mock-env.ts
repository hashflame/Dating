import { emitEvent, mockTelegramEnv } from '@tma.js/sdk-react'

import { getDevUser } from './dev-user'
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

/**
 * Собирается функцией, а не на верхнем уровне модуля: иначе сборщик считает
 * модуль побочно-эффектным и не вырезает его из production-сборки.
 */
function createMockInitData(): string {
  const user = getDevUser()

  return new URLSearchParams([
    [
      'user',
      JSON.stringify({
        id: user.id,
        first_name: user.firstName,
        username: user.username === '' ? undefined : user.username,
        language_code: 'ru',
        // photo_url не подставляем. Вывести его из юзернейма нельзя: проверено
        // 25.08.2026 — `t.me/i/userpic/320/<username>.jpg` отдаёт 404, а `.svg`
        // отдаёт заглушку с инициалами, которую бэкенд всё равно не обработает.
        // Настоящую ссылку знает только клиент Telegram, поэтому шаг «взять фото
        // из Telegram» проверяется внутри клиента; в браузере кнопки просто нет.
      }),
    ],
    ['auth_date', '1716922846'],
    ['signature', 'mock-signature'],
    ['hash', 'mock-hash'],
  ]).toString()
}

/**
 * Подменяет оболочку клиента Telegram, чтобы приложение открывалось в браузере:
 * вне клиента никто не отвечает на запросы SDK, и без этого монтирование темы
 * и вьюпорта зависает или падает с UnknownEnvError.
 *
 * Пользователь при этом настоящий. Подпись, собранная здесь, до бэкенда не
 * доходит: dev-сервер переподписывает initData настоящим токеном бота
 * (`vite/dev-telegram-auth.ts`), оставляя поле `user` как есть.
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
