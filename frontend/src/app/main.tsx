import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'

import { env } from '@/shared/config'
import { initI18n } from '@/shared/i18n'
import { getTelegramUser, initTelegram } from '@/shared/telegram'

import { App } from './App'

import './styles/index.css'

async function bootstrap(): Promise<void> {
  await initTelegram()
  await initI18n(getTelegramUser()?.languageCode)

  // import.meta.env.DEV статически вырезает eruda из production-сборки.
  if (import.meta.env.DEV && env.debugConsole) {
    const eruda = await import('eruda')
    eruda.default.init()
  }

  const container = document.getElementById('root')
  if (!container) {
    throw new Error('Не найден элемент #root')
  }

  createRoot(container).render(
    <StrictMode>
      <App />
    </StrictMode>,
  )
}

void bootstrap()
