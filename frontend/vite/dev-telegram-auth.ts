import { createHmac } from 'node:crypto'

import type { Plugin } from 'vite'

/**
 * Подписывает initData так же, как его проверяет бэкенд
 * (`Blizka.App/Telegram/TelegramInitDataValidator.cs`):
 *
 * 1. строка проверки — поля кроме `hash`, отсортированные по имени, `k=v` через `\n`;
 * 2. секрет — HMAC-SHA256 от токена бота с ключом `WebAppData`;
 * 3. подпись — HMAC-SHA256 от строки проверки с этим секретом.
 */
function signInitData(botToken: string, fields: Record<string, string>): string {
  const dataCheckString = Object.keys(fields)
    .sort()
    .map((key) => `${key}=${fields[key]}`)
    .join('\n')

  const secretKey = createHmac('sha256', 'WebAppData').update(botToken).digest()
  const hash = createHmac('sha256', secretKey).update(dataCheckString).digest('hex')

  const params = new URLSearchParams(fields)
  params.set('hash', hash)

  return params.toString()
}

/** На случай, если клиент прислал запрос без initData вообще. */
const FALLBACK_USER = JSON.stringify({
  id: 99_000_001,
  first_name: 'DevTester',
  language_code: 'ru',
})

/**
 * Берём пользователя из того initData, что прислал клиент: его собирает мок
 * окружения (`shared/telegram/mock-env.ts`), и там же живёт кнопка сброса,
 * которая меняет `user.id`. Если подставлять здесь своего пользователя,
 * сбросить онбординг было бы нечем — бэкенд опознаёт людей по Telegram-id.
 */
function readUser(rawInitData: string | string[] | undefined): string {
  if (typeof rawInitData !== 'string') return FALLBACK_USER

  return new URLSearchParams(rawInitData).get('user') ?? FALLBACK_USER
}

type DevTelegramAuthOptions = {
  /** Значение `TELEGRAM_BOT_TOKEN`. Нет токена — плагин выключен. */
  botToken: string | undefined
  /**
   * Окружение Telegram подменено (`VITE_MOCK_TELEGRAM=1`). Внутри настоящего
   * клиента подписывать нельзя: там приходит подлинный initData реального
   * пользователя, и подмена залогинила бы под dev-аккаунтом.
   */
  mockTelegram: boolean
}

/**
 * Позволяет входить на реальный бэкенд из обычного браузера.
 *
 * Реальный сервер принимает только initData, подписанный токеном бота, поэтому
 * подделка из браузера получает 401. Здесь подпись ставится на стороне
 * dev-сервера: токен читается из `TELEGRAM_BOT_TOKEN` (без префикса `VITE_`,
 * так что в бандл он не попадает) и в браузер не уходит.
 *
 * Алгоритм тот же, что в `backend/scripts/generate-telegram-init-data.js`, —
 * бэкенд не ослабляется и не знает про этот путь.
 *
 * Работает только в `npm run dev`. Выключен без токена или вне режима мока:
 * тогда остаётся обычный путь — локальная заглушка либо запуск внутри Telegram.
 */
export function devTelegramAuth({ botToken, mockTelegram }: DevTelegramAuthOptions): Plugin {
  return {
    name: 'blizka:dev-telegram-auth',
    apply: 'serve',

    configureServer(server) {
      if (!mockTelegram) return

      // Без токена вход вернёт 401, и экран покажет «Не удалось войти».
      // Причина неочевидна, поэтому говорим о ней там, где её увидят.
      if (!botToken) {
        server.config.logger.warn(
          '\n  ⚠  TELEGRAM_BOT_TOKEN не задан — вход на реальный API вернёт 401.' +
            '\n     Впиши в .env токен бота со стенда (Telegram__BotToken в Railway):' +
            '\n     без подписи initData сервер не пускает. Подробнее: docs/real-backend.md\n',
        )

        return
      }

      server.middlewares.use('/api/auth/telegram', (req, _res, next) => {
        req.headers['x-telegram-initdata'] = signInitData(botToken, {
          auth_date: String(Math.floor(Date.now() / 1000)),
          user: readUser(req.headers['x-telegram-initdata']),
        })

        next()
      })

      server.config.logger.info('\n  [32m➜[0m  Telegram initData подписывается для dev-входа')
    },
  }
}
