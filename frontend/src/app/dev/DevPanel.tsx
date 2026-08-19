import { env } from '@/shared/config'

import { DevLocaleToggle } from './DevLocaleToggle'
import { DevThemeToggle } from './DevThemeToggle'

/**
 * Панель инструментов разработки: тема и язык.
 *
 * Рендерится только при подменённом окружении Telegram, а из production-сборки
 * вырезается целиком проверкой `import.meta.env.DEV` в App.
 */
export function DevPanel() {
  if (!env.mockTelegram) return null

  return (
    <div className="pointer-events-none fixed top-0 right-3 z-50 flex flex-col items-end gap-1 pt-safe">
      <div className="pointer-events-auto mt-2 flex gap-1">
        <DevLocaleToggle />
        <DevThemeToggle />
      </div>
    </div>
  )
}
