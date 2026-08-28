import {
  backButton,
  init as initSdk,
  isTMA,
  initData,
  miniApp,
  swipeBehavior,
  themeParams,
  viewport,
} from '@tma.js/sdk-react'

import { env } from '@/shared/config'

import { mockTelegramEnvironment } from './mock-env'

/**
 * Приложение открыли не из Telegram.
 *
 * Отдельный тип ошибки, потому что это не сбой, а нормальная ситуация:
 * вне клиента нет ни данных пользователя, ни темы, ни вьюпорта, и показать
 * надо не «что-то пошло не так», а объяснение с выходом.
 */
export class OutsideTelegramError extends Error {
  constructor() {
    super('OUTSIDE_TELEGRAM')
    this.name = 'OutsideTelegramError'
  }
}

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

  // Проверяем до initSdk(): дальше SDK начнёт спрашивать у клиента тему и
  // вьюпорт, и без него всё упадёт невнятным UnknownEnvError.
  // `isTMA()` вне клиента не возвращает false, а бросает — отсюда try.
  if (!isInsideTelegram()) throw new OutsideTelegramError()

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

  // Без expand() клиент может открыть мини-апп не на полную высоту —
  // тогда нижние кнопки оказываются за пределами видимой области.
  if (viewport.expand.isAvailable()) viewport.expand()

  syncColorScheme()

  miniApp.ready()
}

function isInsideTelegram(): boolean {
  try {
    return isTMA()
  } catch {
    return false
  }
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
  // Вне Telegram (или до успешной инициализации) обращение к SDK бросает —
  // а вызывающему достаточно знать, что пользователя нет.
  const user = isInsideTelegram() ? initData.user() : undefined
  if (!user) return undefined

  return {
    id: user.id,
    firstName: user.first_name,
    languageCode: user.language_code,
    photoUrl: user.photo_url,
  }
}
