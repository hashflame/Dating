import { type i18n } from 'i18next'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'

import { env } from '@/shared/config'
import { initI18n } from '@/shared/i18n'
import { getTelegramUser, initTelegram, OutsideTelegramError } from '@/shared/telegram'
import { ErrorState } from '@/shared/ui'

import { App } from './App'
import { ErrorBoundary } from './ErrorBoundary'

import './styles/index.css'

/**
 * Переводчик появляется только после `initI18n`, а падать можно и раньше.
 * Поэтому держим ссылку: пока её нет, показываем текст ошибки как есть.
 */
let translate: i18n['t'] | null = null

function renderFatalError(root: ReturnType<typeof createRoot>, reason: unknown): void {
  // Открыли не из Telegram — это не сбой, а понятная ситуация: показываем
  // объяснение с выходом, а не имя внутренней ошибки SDK.
  if (reason instanceof OutsideTelegramError && translate !== null) {
    root.render(
      <ErrorState
        title={translate('outsideTelegram.title')}
        description={translate('outsideTelegram.description')}
      />,
    )

    return
  }

  root.render(
    <ErrorState
      description={reason instanceof Error ? `${reason.name}: ${reason.message}` : String(reason)}
    />,
  )
}

async function bootstrap(): Promise<void> {
  const container = document.getElementById('root')
  if (!container) {
    throw new Error('Не найден элемент #root')
  }
  const root = createRoot(container)

  // ErrorBoundary ниже ловит только ошибки рендера — необработанный reject промиса
  // (например, у `navigate()` в эффекте) он не видит, и без этого перехватчика
  // Telegram-клиент увидит "зависшую" вкладку и покажет свой нативный fallback.
  window.addEventListener('unhandledrejection', (event) => {
    renderFatalError(root, event.reason)
  })

  // initTelegram() ловим отдельно: даже если он упал, i18n должен успеть
  // инициализироваться до renderFatalError — иначе ErrorState покажет
  // непереведённый ключ вместо текста ошибки.
  let telegramError: unknown = null
  try {
    await initTelegram()
  } catch (error) {
    telegramError = error
  }

  // Язык берём у Telegram только если он поднялся: иначе обращение к SDK
  // бросит ту же ошибку и затрёт понятное сообщение.
  const language = telegramError === null ? getTelegramUser()?.languageCode : undefined
  translate = (await initI18n(language)).t

  if (telegramError) {
    renderFatalError(root, telegramError)
    return
  }

  try {
    // `import.meta.env.DEV` — статическая константа, поэтому в production ветка
    // вырезается целиком вместе с чанком eruda (полмегабайта). Обращение
    // к `env.debugConsole` сборщик свернуть не может: это поле объекта.
    if (import.meta.env.DEV && env.debugConsole) {
      const eruda = await import('eruda')
      eruda.default.init()
    }

    root.render(
      <StrictMode>
        <ErrorBoundary>
          <App />
        </ErrorBoundary>
      </StrictMode>,
    )
  } catch (error) {
    // Сбой до рендера — ErrorBoundary тут не поможет, дерево ещё не смонтировано.
    renderFatalError(root, error)
  }
}

void bootstrap()
