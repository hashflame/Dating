import type { Plugin } from 'vite'

const SECRET_HEADER = 'x-dev-login-secret'
const TELEGRAM_ID_HEADER = 'x-dev-login-telegramid'

/**
 * Достаёт `user.id` из заголовка `X-Telegram-InitData`, которое собирает
 * мок Telegram-окружения (`shared/telegram/mock-env.ts`). Тот же приём,
 * что и `readUser()` в `dev-telegram-auth.ts`, только глубже — там нужна
 * вся строка initData, здесь только id для замены на dev-login-заголовки.
 */
function readUserId(rawInitData: string | string[] | undefined): string | null {
  if (typeof rawInitData !== 'string') return null

  const raw = new URLSearchParams(rawInitData).get('user')
  if (!raw) return null

  try {
    const user = JSON.parse(raw) as { id?: unknown }
    return typeof user.id === 'number' ? String(user.id) : null
  } catch {
    return null
  }
}

type DevLoginAuthOptions = {
  /** Значение `DEV_LOGIN_SECRET`. Нет секрета — плагин выключен. */
  secret: string | undefined
  /** Как и в `dev-telegram-auth.ts`: работает только при подмене окружения Telegram. */
  mockTelegram: boolean
}

/**
 * ВРЕМЕННЫЙ инструмент только для ручного тестирования на демо-данных
 * (backend: `docs/specs/003-demo-seed-data.md`). Убрать, когда демо-окружение
 * станет не нужно.
 *
 * Демо-аккаунт выбирается полем «Telegram-id» в панели разработки: подходят
 * только id `990000000001`–`990000000010`, остальные бэкенд отвергает.
 *
 * Бэкенд принимает на `POST /api/auth/telegram` заголовки
 * `X-Dev-Login-Secret` + `X-Dev-Login-TelegramId` вместо подписанного initData —
 * и пускает только под одним из 10 фиксированных демо-аккаунтов, не под
 * произвольным Telegram-id. В отличие от `dev-telegram-auth.ts` не нужен
 * `TELEGRAM_BOT_TOKEN`: секрет из `.env` подставляется на стороне dev-сервера
 * и в браузер не уходит — так же, как подпись initData там.
 */
export function devLoginAuth({ secret, mockTelegram }: DevLoginAuthOptions): Plugin {
  return {
    name: 'blizka:dev-login-auth',
    apply: 'serve',

    configureServer(server) {
      if (!mockTelegram || !secret) return

      server.middlewares.use('/api/auth/telegram', (req, _res, next) => {
        const telegramId = readUserId(req.headers['x-telegram-initdata'])
        if (telegramId !== null) {
          delete req.headers['x-telegram-initdata']
          req.headers[SECRET_HEADER] = secret
          req.headers[TELEGRAM_ID_HEADER] = telegramId
        }

        next()
      })

      server.config.logger.info(
        '\n  [33m➜[0m  DEV_LOGIN_SECRET задан — вход идёт по демо-аккаунтам, в обход initData\n',
      )
    },
  }
}
