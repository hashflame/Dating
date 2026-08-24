import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'

import { env } from '@/shared/config'
import { initI18n } from '@/shared/i18n'
import { getTelegramUser, initTelegram } from '@/shared/telegram'
import { ErrorState } from '@/shared/ui'

import { App } from './App'
import { ErrorBoundary } from './ErrorBoundary'

import './styles/index.css'

async function bootstrap(): Promise<void> {
  const container = document.getElementById('root')
  if (!container) {
    throw new Error('Не найден элемент #root')
  }
  const root = createRoot(container)

  try {
    await initTelegram()
    await initI18n(getTelegramUser()?.languageCode)

    // import.meta.env.DEV статически вырезает eruda из production-сборки.
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
    // Сбой до рендера (например, инициализация Telegram SDK) — ErrorBoundary тут
    // не поможет, дерево ещё не смонтировано.
    root.render(
      <ErrorState
        description={error instanceof Error ? `${error.name}: ${error.message}` : String(error)}
      />,
    )
  }
}

void bootstrap()
