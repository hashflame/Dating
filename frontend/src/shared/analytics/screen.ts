import { ROUTES } from '@/shared/config'

import { type ScreenName } from './events'

const ENTRIES = Object.entries(ROUTES) as Array<[ScreenName, string]>

/**
 * Имя экрана по пути роутера.
 *
 * Путь в аналитику не уходит: у хаба мэтча он содержит id (`/matches/7c5f…`),
 * и в PostHog это дало бы по отдельному «экрану» на каждую пару. Сегменты
 * с параметром (`$matchId`) сопоставляются как шаблон.
 *
 * `null` — путь не из `ROUTES`: такого быть не должно, и выдумывать имя
 * лучше не надо, чем гадать.
 */
export function screenFromPath(pathname: string): ScreenName | null {
  const parts = pathname.split('/')

  // Точное совпадение важнее шаблона: статический путь не должен доставаться
  // роуту с параметром той же длины.
  const exact = ENTRIES.find(([, pattern]) => pattern === pathname)
  if (exact) return exact[0]

  const matched = ENTRIES.find(([, pattern]) => {
    const patternParts = pattern.split('/')

    return (
      patternParts.length === parts.length &&
      patternParts.every((segment, index) => segment.startsWith('$') || segment === parts[index])
    )
  })

  return matched?.[0] ?? null
}
