import { Check } from 'lucide-react'
import { type ReactNode } from 'react'

import { cn } from '@/shared/lib'

import { ToggleGroupItem } from './kit/toggle-group'

type OptionCardProps = {
  /** Значение для родительского `ToggleGroup`. */
  value: string
  label: string
  /** Эмодзи или иконка в плашке слева. */
  icon?: ReactNode
  /** Показывать галочку выбранного в углу. Для одиночного выбора обычно не нужна. */
  withCheck?: boolean
  disabled?: boolean
  className?: string
}

/**
 * Крупный выбираемый вариант: иконка в плашке, подпись в две строки,
 * галочка выбранного. Высота фиксирована — карточки в сетке ровные
 * независимо от длины текста.
 */
export function OptionCard({
  value,
  label,
  icon,
  withCheck = true,
  disabled,
  className,
}: OptionCardProps) {
  return (
    <ToggleGroupItem
      value={value}
      disabled={disabled}
      variant="surface"
      size="none"
      // `min-h`, а не `h`: на узких экранах «Общение и переписка» не влезает
      // в две строки, и фиксированная высота обрезала бы текст многоточием.
      className={cn('group min-h-16 w-full justify-start gap-3 px-3 py-2 text-left', className)}
    >
      {icon && (
        <span
          className={cn(
            'flex size-9 shrink-0 items-center justify-center rounded-full bg-muted text-base leading-none text-muted-foreground transition-colors duration-150',
            'group-data-[state=on]:bg-brand group-data-[state=on]:text-brand-foreground',
          )}
          aria-hidden
        >
          {icon}
        </span>
      )}

      {/* `break-words`: без него длинное слово («Серьёзные») не переносится,
          вылезает за карточку и обрезается посреди буквы. */}
      <span className="flex-1 text-sm leading-tight font-semibold break-words">{label}</span>

      {withCheck && (
        <Check
          className="size-4 shrink-0 text-brand opacity-0 transition-opacity duration-150 group-data-[state=on]:opacity-100"
          aria-hidden
        />
      )}
    </ToggleGroupItem>
  )
}
