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
 */
export function Card({ children, padding = 'default', className }: CardProps) {
  return (
    <CardPrimitive
      className={cn('block gap-0 rounded-lg shadow-none', PADDING[padding], className)}
    >
      {children}
    </CardPrimitive>
  )
}
