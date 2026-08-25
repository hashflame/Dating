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

/**
 * Берём пользователя из того initData, что прислал клиент: его собирает
 * `shared/telegram/dev-user.ts`, и там же живёт выбор аккаунта в панели
 * разработки. Своего пользователя здесь не подставляем: бэкенд опознаёт людей
 * по Telegram-id, и подмена увела бы работу на чужой аккаунт незаметно.
 *
 * `null` — клиент не прислал `user`. Подписывать нечего, пусть запрос уйдёт
 * как есть и вернётся 401: это честнее выдуманного пользователя.
 */
function readUser(rawInitData: string | string[] | undefined): string | null {
  if (typeof rawInitData !== 'string') return null

  return new URLSearchParams(rawInitData).get('user')
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
        const user = readUser(req.headers['x-telegram-initdata'])

        if (user === null) {
          server.config.logger.warn(
            '  ⚠  Запрос на вход пришёл без пользователя в initData — подписывать нечего.',
          )
        } else {
          req.headers['x-telegram-initdata'] = signInitData(botToken, {
            auth_date: String(Math.floor(Date.now() / 1000)),
            user,
          })
        }

        next()
      })

      server.config.logger.info('\n  [32m➜[0m  Telegram initData подписывается для dev-входа')
    },
  }
}
