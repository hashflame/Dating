import {
  backButton,
  init as initSdk,
  initData,
  miniApp,
  swipeBehavior,
  themeParams,
  viewport,
} from '@tma.js/sdk-react'

import { env } from '@/shared/config'

import { mockTelegramEnvironment } from './mock-env'

/**
 * Вызывается один раз до рендера. Порядок важен: init → монтирование → ready.
 * После этого на :root есть `--tg-theme-*` и `--tg-viewport-*`, на которых
 * построены токены темы (src/app/styles/index.css).
 */
export async function initTelegram(): Promise<void> {
  // import.meta.env.DEV статически вырезает мок из production-сборки.
  if (import.meta.env.DEV && env.mockTelegram) {
    mockTelegramEnvironment()
  }

  initSdk()

  themeParams.mount()
  themeParams.bindCssVars()

  miniApp.mount()
  miniApp.bindCssVars()

  backButton.mount()

  // Вертикальный свайп закрывает мини-апп и мешает свайпам ленты.
  swipeBehavior.mount()
  swipeBehavior.disableVertical()

  await viewport.mount()
  viewport.bindCssVars()

  syncColorScheme()

  miniApp.ready()
}

/** Держит класс `.dark` на <html> в соответствии с темой клиента. */
function syncColorScheme(): void {
  const apply = (): void => {
    document.documentElement.classList.toggle('dark', miniApp.isDark())
  }

  apply()
  miniApp.isDark.sub(apply)
}

/** Telegram-пользователь из launch params. `undefined` вне Telegram. */
export function getTelegramUser(): { id: number; languageCode: string | undefined } | undefined {
  const user = initData.user()
  if (!user) return undefined

  return { id: user.id, languageCode: user.language_code }
}
