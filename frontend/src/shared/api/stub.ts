import { env } from '@/shared/config'

/**
 * Заглушка для эндпоинта, которого ещё нет на бэкенде.
 *
 * Правила использования:
 * 1. Проверь, что эндпоинта действительно нет — поищи его в папке `backend/`.
 * 2. Оставь рядом комментарий `// @stub: <причина>`.
 * 3. Добавь строку в `docs/api-gaps.md`.
 * 4. Когда эндпоинт появится — удали заглушку и строку из `docs/api-gaps.md`.
 *
 * В production-сборке заглушка падает: незамеченных заглушек в релизе быть не должно.
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
