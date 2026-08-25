import { SlidersHorizontal, X } from 'lucide-react'
import { useState } from 'react'

import { env } from '@/shared/config'
import { Card } from '@/shared/ui'

import { DevLocaleToggle } from './DevLocaleToggle'
import { DevResetButtons } from './DevResetButtons'
import { DevThemeToggle } from './DevThemeToggle'
import { DevUserForm } from './DevUserForm'

/**
 * Панель инструментов разработки: язык, тема, от кого работаем и сбросы.
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
    <div className="pointer-events-none fixed top-0 right-3 z-50 flex flex-col items-end gap-2 pt-safe">
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
        <Card padding="tight" className="pointer-events-auto flex w-60 flex-col gap-3 shadow-lg">
          <div className="flex flex-wrap items-center gap-1">
            <DevLocaleToggle />
            <DevThemeToggle />
          </div>

          <DevUserForm />
          <DevResetButtons />
        </Card>
      )}
    </div>
  )
}
