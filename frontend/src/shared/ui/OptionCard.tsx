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
      // Без иконки карточке не нужен простор под плашку — только текст
      // и, если есть, галочка в углу, поэтому высота ниже.
      className={cn(
        'group relative w-full justify-start gap-3 rounded-md px-3.5 text-left',
        icon ? 'min-h-18 py-3' : 'min-h-11 py-2.5',
        // Место под галочку держим всегда: она появляется в углу, и без
        // резерва длинная подпись выбранной карточки заезжала бы под неё.
        withCheck && 'pr-9',
        className,
      )}
    >
      {icon && (
        <span
          className={cn(
            'flex size-9 shrink-0 items-center justify-center rounded-full bg-surface-strong text-base leading-none text-muted-foreground transition-colors duration-150',
            // Карточка целиком залита фирменным — плашка отделяется от неё
            // прозрачным белым, иначе на красном она читается как дырка.
            'group-data-[state=on]:bg-brand-foreground/20 group-data-[state=on]:text-brand-foreground',
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
        <span
          className="absolute top-2.5 right-2.5 flex size-5 items-center justify-center rounded-full bg-brand-foreground text-brand opacity-0 transition-opacity duration-150 group-data-[state=on]:opacity-100"
          aria-hidden
        >
          <Check className="size-3.5 stroke-[3]" />
        </span>
      )}
    </ToggleGroupItem>
  )
}
