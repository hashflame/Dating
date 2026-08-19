import { env } from '@/shared/config'

/**
 * Заглушка эндпоинта, которого ещё нет на бэкенде.
 * Порядок: проверить, что эндпоинта правда нет → комментарий `// @stub: причина`
 * → строка в `docs/api-gaps.md`. В production бросает: забытая заглушка упадёт
 * на сборке, а не тихо уедет в релиз.
 */
export function stub<T>(label: string, data: T, delayMs = 300): Promise<T> {
  if (!env.isDev) {
    return Promise.reject(new Error(`Заглушка «${label}» попала в production-сборку`))
  }

  console.warn(`[stub] ${label} — эндпоинт ещё не реализован на бэкенде`)

  return new Promise((resolve) => {
    setTimeout(() => resolve(data), delayMs)
  })
}
