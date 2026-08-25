import type { Plugin } from 'vite'

type DevUserEnv = {
  id: string | undefined
  name: string | undefined
  username: string | undefined
}

/**
 * Подставляет в код константу `__DEV_USER__` — аккаунт, под которым по умолчанию
 * входим из браузера (`DEV_USER_ID`, `DEV_USER_NAME`, `DEV_USER_USERNAME` в `.env`).
 *
 * Почему не через `VITE_*`: Vite подставляет вместо `import.meta.env` объект со
 * всеми `VITE_`-переменными, поэтому личный Telegram-id разработчика попадал бы
 * в production-бандл — проверено, попадал. `apply: 'serve'` гарантирует, что в
 * прод-сборке константы нет вовсе.
 */
export function devUserDefine({ id, name, username }: DevUserEnv): Plugin {
  const parsed = Number(id)
  const user =
    Number.isSafeInteger(parsed) && parsed > 0
      ? { id: parsed, firstName: name ?? '', username: username ?? '' }
      : null

  return {
    name: 'blizka:dev-user-define',
    apply: 'serve',
    config: () => ({ define: { __DEV_USER__: JSON.stringify(user) } }),
  }
}
