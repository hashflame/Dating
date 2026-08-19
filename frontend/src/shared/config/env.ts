import { z } from 'zod'

const flag = z.enum(['0', '1']).default('0')

const envSchema = z.object({
  VITE_API_BASE_URL: z.string().default(''),
  VITE_DEBUG_CONSOLE: flag,
  VITE_MOCK_TELEGRAM: flag,
})

const parsed = envSchema.parse(import.meta.env)

/** Единственная точка чтения переменных окружения. `import.meta.env` больше нигде не используется. */
export const env = {
  apiBaseUrl: parsed.VITE_API_BASE_URL,
  /** Показывать мобильную dev-консоль внутри Telegram. */
  debugConsole: parsed.VITE_DEBUG_CONSOLE === '1',
  /** Подменять окружение Telegram, чтобы приложение открывалось в обычном браузере. */
  mockTelegram: parsed.VITE_MOCK_TELEGRAM === '1',
  isDev: import.meta.env.DEV,
} as const
