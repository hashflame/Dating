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
 * в production-бандл — проверено, попадал.
 *
 * Плагин работает и на `vite build` (не только `apply: 'serve'`): ВРЕМЕННЫЙ
 * задеплоенный dev-стенд фронта (`VITE_DEV_LOGIN_SECRET`, см. `docs/real-backend.md`)
 * собирается через `vite build` с `NODE_ENV=development`, чтобы остался жив
 * `import.meta.env.DEV`-код (панель разработки и т.д.) — без определения здесь
 * `__DEV_USER__` остался бы неразрешённой ссылкой в этой сборке. На настоящей
 * production-сборке (`NODE_ENV` не `development`) ветка `import.meta.env.DEV`
 * в `env.ts` вырезается сборщиком целиком, включая обращение к константе, —
 * туда ничего не попадает независимо от того, где выполняется этот плагин.
 */
export function devUserDefine({ id, name, username }: DevUserEnv): Plugin {
  const parsed = Number(id)
  const user =
    Number.isSafeInteger(parsed) && parsed > 0
      ? { id: parsed, firstName: name ?? '', username: username ?? '' }
      : null

  return {
    name: 'blizka:dev-user-define',
    config: () => ({ define: { __DEV_USER__: JSON.stringify(user) } }),
  }
}
