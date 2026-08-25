/// <reference types="vite/client" />

/**
 * Аккаунт для входа из браузера, подставленный dev-сервером
 * (`vite/dev-user-define.ts`) из `DEV_USER_*` в `.env`.
 *
 * Переменные без префикса `VITE_` намеренно: с префиксом Vite вшил бы их
 * значения в production-бандл вместе со всем `import.meta.env`. Здесь их
 * подставляет плагин с `apply: 'serve'`, поэтому в прод-сборке константы нет,
 * а обращение к ней вырезается вместе с ветками `import.meta.env.DEV`.
 */
declare const __DEV_USER__: { id: number; firstName: string; username: string } | null
