import { clsx, type ClassValue } from 'clsx'
import { extendTailwindMerge } from 'tailwind-merge'

/**
 * Наши размеры шрифта из `@theme` (`--text-*` в app/styles/index.css).
 *
 * Без этого списка tailwind-merge не знает, что `text-micro` — размер, и
 * принимает его за цвет: в паре с `text-brand` один из классов молча
 * выбрасывается. Ошибка не падает, просто текст оказывается не того размера,
 * поэтому список нужно пополнять вместе с токенами.
 */
const FONT_SIZES = ['display', 'tiny', 'micro'] as const

const twMerge = extendTailwindMerge({
  extend: {
    classGroups: {
      'font-size': [{ text: [...FONT_SIZES] }],
    },
  },
})

/** Склеивает Tailwind-классы, разрешая конфликты в пользу последнего. */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs))
}
