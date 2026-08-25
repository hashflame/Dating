import { Check } from 'lucide-react'
import { type ReactNode } from 'react'

import { cn } from '@/shared/lib'

type ListRowProps = {
  title: string
  /** Вторая строка под названием. */
  subtitle?: string
  /** Слева: аватар, иконка, флаг. */
  leading?: ReactNode
  /** Справа: сумма, значение, шеврон. */
  trailing?: ReactNode
  selected?: boolean
  onClick?: () => void
  className?: string
}

/**
 * Строка списка: название, подпись, галочка выбранного.
 * Общая для поиска городов, интересов, мэтчей и симпатий.
 */
export function ListRow({
  title,
  subtitle,
  leading,
  trailing,
  selected = false,
  onClick,
  className,
}: ListRowProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={selected}
      className={cn(
        'flex min-h-14 w-full items-center gap-3 border-b border-border px-4 text-left outline-none last:border-b-0',
        'transition-colors duration-150 hover:bg-accent focus-visible:bg-accent',
        selected && 'bg-brand-soft hover:bg-brand-soft',
        className,
      )}
    >
      {leading}

      <span className="min-w-0 flex-1">
        <span
          className={cn(
            'block truncate text-sm font-semibold',
            selected ? 'text-brand' : 'text-foreground',
          )}
        >
          {title}
        </span>
        {subtitle && <span className="block truncate text-tiny text-faint">{subtitle}</span>}
      </span>

      {trailing && <span className="shrink-0 text-sm font-semibold">{trailing}</span>}

      {selected && <Check className="size-4 shrink-0 text-brand" aria-hidden />}
    </button>
  )
}
