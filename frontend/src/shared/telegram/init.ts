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

  // Без restore() данные пользователя из launch params недоступны:
  // не будет ни языка, ни имени, ни аватара.
  initData.restore()

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

type TelegramUser = {
  id: number
  firstName: string
  languageCode: string | undefined
  /** Аватар из Telegram: сервер его не хранит, клиент присылает при импорте фото. */
  photoUrl: string | undefined
}

/** Telegram-пользователь из launch params. `undefined` вне Telegram. */
export function getTelegramUser(): TelegramUser | undefined {
  const user = initData.user()
  if (!user) return undefined

  return {
    id: user.id,
    firstName: user.first_name,
    languageCode: user.language_code,
    photoUrl: user.photo_url,
  }
}
