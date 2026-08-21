import { SlidersHorizontal, X } from 'lucide-react'
import { useState } from 'react'

import { env } from '@/shared/config'

import { DevLocaleToggle } from './DevLocaleToggle'
import { DevThemeToggle } from './DevThemeToggle'

/**
 * Панель инструментов разработки: тема и язык.
 *
 * Свёрнута по умолчанию: раскрытая она перекрывает шапку экрана (кнопку
 * «Назад» и точки шагов) и мешает смотреть вёрстку.
 *
 * Рендерится только при подменённом окружении Telegram, а из production-сборки
 * вырезается целиком проверкой `import.meta.env.DEV` в App.
 */
export function DevPanel() {
  const [open, setOpen] = useState(false)

  if (!env.mockTelegram) return null

  return (
    <div className="pointer-events-none fixed top-0 right-3 z-50 flex flex-col items-end gap-1 pt-safe">
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        aria-label={open ? 'Скрыть панель разработки' : 'Показать панель разработки'}
        aria-expanded={open}
        className="pointer-events-auto mt-2 flex size-9 items-center justify-center rounded-full border border-border bg-card/90 text-muted-foreground shadow-sm backdrop-blur transition-colors hover:bg-accent"
      >
        {open ? (
          <X className="size-4" aria-hidden />
        ) : (
          <SlidersHorizontal className="size-4" aria-hidden />
        )}
      </button>

      {open && (
        <div className="pointer-events-auto flex flex-col items-end gap-1">
          <DevLocaleToggle />
          <DevThemeToggle />
        </div>
      )}
    </div>
  )
}
