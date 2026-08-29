import { z } from 'zod'

// .catch() (не .default()) — ловит и отсутствующее, и невалидное значение (например,
// пустую строку из-за неверно настроенной переменной в Railway). Модуль читается на
// самом старте приложения, до перехватчиков ошибок в main.tsx: упавший здесь parse()
// уронит весь скрипт ещё до монтирования React, и Telegram покажет свой нативный
// экран краша вместо нашего ErrorState.
const flag = z.enum(['0', '1']).catch('0')

const envSchema = z.object({
  VITE_API_BASE_URL: z.string().default(''),
  VITE_DEBUG_CONSOLE: flag,
  VITE_MOCK_TELEGRAM: flag,
  VITE_DEV_LOGIN_SECRET: z.string().default(''),
  VITE_POSTHOG_KEY: z.string().default(''),
  VITE_POSTHOG_HOST: z.string().default('https://eu.i.posthog.com'),
})

const parsed = envSchema.parse(import.meta.env)

/** Единственная точка чтения переменных окружения. `import.meta.env` больше нигде не используется. */
export const env = {
  apiBaseUrl: parsed.VITE_API_BASE_URL,
  /** Показывать мобильную dev-консоль внутри Telegram. */
  debugConsole: parsed.VITE_DEBUG_CONSOLE === '1',
  /**
   * Подменять оболочку клиента Telegram (тема, вьюпорт, хаптика), чтобы
   * приложение открывалось в обычном браузере. Пользователь при этом
   * настоящий — см. `devUser` ниже.
   */
  mockTelegram: parsed.VITE_MOCK_TELEGRAM === '1',
  /**
   * От чьего имени входим в браузере по умолчанию: `DEV_USER_*` из `.env`.
   * Личный аккаунт разработчика, у каждого свой, в репозиторий не уезжает.
   *
   * Подставляется dev-сервером (`vite/dev-user-define.ts`), а не через
   * `VITE_`-переменную: та вшила бы Telegram-id в production-бандл. Ветка
   * `import.meta.env.DEV` статически вырезает обращение из прод-сборки.
   */
  devUser: import.meta.env.DEV ? __DEV_USER__ : null,
  isDev: import.meta.env.DEV,
  /**
   * ВРЕМЕННО (backend: `docs/specs/003-demo-seed-data.md`). Секрет для входа
   * по демо-аккаунтам без initData, встроенный прямо в бандл — единственный
   * способ достать его до браузера на задеплоенном dev-стенде фронта, где нет
   * dev-сервера, который спрятал бы его на стороне Node (там для этого свой,
   * несвязанный `DEV_LOGIN_SECRET` — см. `vite/dev-login-auth.ts`).
   * Пустая строка на обычном локальном dev и в проде — фича выключена.
   */
  devLoginSecret: parsed.VITE_DEV_LOGIN_SECRET,
  /**
   * PostHog. Пустой ключ = аналитика выключена: SDK не грузится и ни одного
   * запроса наружу не уходит. Так по умолчанию в dev и в любой сборке, где
   * переменную не задали, — включается только заданием ключа на Railway.
   *
   * Регион EU: пользователи из Беларуси и ЕС, данные не должны уезжать в США.
   */
  analytics: {
    key: parsed.VITE_POSTHOG_KEY,
    host: parsed.VITE_POSTHOG_HOST,
    enabled: parsed.VITE_POSTHOG_KEY !== '',
  },
} as const
