import { type ReactNode } from 'react'

import { cn } from '@/shared/lib'

type TagProps = {
  children: ReactNode
  /** Совпадение с текущим пользователем — подсвечиваем фирменным. */
  highlighted?: boolean
  className?: string
}

/**
 * Неинтерактивная метка: интерес, ценность, цель знакомства.
 * Для выбираемых вариантов есть `SegmentedControl` и `OptionCard` — это не они.
 */
export function Tag({ children, highlighted = false, className }: TagProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-tiny leading-none whitespace-nowrap',
        highlighted ? 'bg-brand-soft font-semibold text-brand' : 'bg-tag text-muted-foreground',
        className,
      )}
    >
      {children}
    </span>
  )
}
