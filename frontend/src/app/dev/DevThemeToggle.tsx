import { Monitor, Moon, Sun, type LucideIcon } from 'lucide-react'
import { useState } from 'react'

import { cn } from '@/shared/lib'
import { getMockColorScheme, setMockColorScheme, type MockColorScheme } from '@/shared/telegram'

type Option = {
  value: MockColorScheme
  label: string
  Icon: LucideIcon
}

const OPTIONS: readonly Option[] = [
  { value: 'system', label: 'Тема как в системе', Icon: Monitor },
  { value: 'light', label: 'Светлая тема', Icon: Sun },
  { value: 'dark', label: 'Тёмная тема', Icon: Moon },
]

/**
 * Переключатель темы для проверки цветов в браузере.
 *
 * Внутри Telegram его нет: там тему задаёт клиент по настройке телефона,
 * и переопределять её приложением нельзя.
 */
export function DevThemeToggle() {
  const [scheme, setScheme] = useState<MockColorScheme>(getMockColorScheme)

  const handleSelect = (next: MockColorScheme): void => {
    setMockColorScheme(next)
    setScheme(next)
  }

  return (
    <div className="flex gap-1" role="group" aria-label="Тема (только для разработки)">
      {OPTIONS.map(({ value, label, Icon }) => (
        <button
          key={value}
          type="button"
          onClick={() => handleSelect(value)}
          aria-label={label}
          aria-pressed={scheme === value}
          className={cn(
            'flex size-9 items-center justify-center rounded-full border border-border bg-card/90 shadow-sm backdrop-blur transition-colors',
            scheme === value
              ? 'bg-primary text-primary-foreground'
              : 'text-muted-foreground hover:bg-accent',
          )}
        >
          <Icon className="size-4" aria-hidden />
        </button>
      ))}
    </div>
  )
}
