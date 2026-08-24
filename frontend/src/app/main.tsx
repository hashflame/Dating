import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'

import { env } from '@/shared/config'
import { initI18n } from '@/shared/i18n'
import { getTelegramUser, initTelegram } from '@/shared/telegram'
import { ErrorState } from '@/shared/ui'

import { App } from './App'
import { ErrorBoundary } from './ErrorBoundary'

import './styles/index.css'

function renderFatalError(root: ReturnType<typeof createRoot>, reason: unknown): void {
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

  await initI18n(getTelegramUser()?.languageCode)

  if (telegramError) {
    renderFatalError(root, telegramError)
    return
  }

  try {
    if (env.debugConsole) {
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
