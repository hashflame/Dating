import { type ReactNode } from 'react'

import { cn } from '@/shared/lib'

export type IconProps = {
  className?: string
}

type BaseIconProps = IconProps & {
  children: ReactNode
}

/**
 * Общая обвязка своих иконок: сетка 24×24, контур в `currentColor`, скруглённые
 * концы. Метрика ровно как у `lucide-react` — штрих 2, рисунок в поле 2…22, —
 * потому что своими закрыты только пробелы в их наборе: в одной строке иконки
 * из двух наборов не должны различаться ни весом, ни размером.
 *
 * Размер задаётся классом (`size-5`), цвет наследуется от родителя: иконка
 * подставляется в готовую разметку без правки самой разметки.
 */
export function Icon({ className, children }: BaseIconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={cn('size-5 shrink-0', className)}
      aria-hidden
      focusable="false"
    >
      {children}
    </svg>
  )
}
