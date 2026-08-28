import { type ReactNode } from 'react'

import { cn } from '@/shared/lib'

import { Card as CardPrimitive } from './kit/card'

type CardProps = {
  children: ReactNode
  /** `tight` — плотная карточка-подсказка из макетов, `default` — обычная секция. */
  padding?: 'default' | 'tight' | 'none'
  className?: string
}

const PADDING = {
  default: 'p-5',
  tight: 'p-3.5',
  none: 'p-0',
} as const

/**
 * Карточка из макетов на основе примитива shadcn.
 * `block` перебивает его `flex flex-col`: раскладку задаёт вызывающий код,
 * иначе содержимое неожиданно выстраивается в колонку.
 *
 * Ни рамки, ни тени: карточку отделяет от фона только заливка `--surface`
 * и крупный радиус. Поэтому же `bg-surface`, а не `bg-card` — тема Telegram
 * часто отдаёт `section_bg_color` равным фону, и карточка на нём пропадала бы.
 */
export function Card({ children, padding = 'default', className }: CardProps) {
  return (
    <CardPrimitive
      className={cn(
        'block gap-0 rounded-lg border-0 bg-surface shadow-none',
        PADDING[padding],
        className,
      )}
    >
      {children}
    </CardPrimitive>
  )
}
