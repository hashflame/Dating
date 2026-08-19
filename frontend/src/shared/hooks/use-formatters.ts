import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'

import { formatDate, formatDistanceKm, formatRelativeTime } from '@/shared/lib'

type Formatters = {
  /** «12 мая» */
  date: (value: string | Date, options?: Intl.DateTimeFormatOptions) => string
  /** «5 минут назад» */
  relativeTime: (value: string | Date) => string
  /** «800 м», «2,4 км» */
  distanceKm: (km: number) => string
}

/**
 * Форматтеры, привязанные к текущему языку интерфейса.
 * Используй их вместо ручной передачи локали в функции из `shared/lib`.
 */
export function useFormatters(): Formatters {
  const { i18n } = useTranslation()
  const locale = i18n.language

  return useMemo<Formatters>(
    () => ({
      date: (value, options) => formatDate(value, locale, options),
      relativeTime: (value) => formatRelativeTime(value, locale),
      distanceKm: (km) => formatDistanceKm(km, locale),
    }),
    [locale],
  )
}
