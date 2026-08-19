import { ROUTES } from '@/shared/config'

import { type Session } from '../types/session'

type StartRoute = typeof ROUTES.welcome | typeof ROUTES.home

/**
 * Пока различаем только «новый» и «остальные»: экранов онбординга ещё нет.
 * Про повторный показ согласия — см. docs/api-gaps.md.
 */
export function resolveStartRoute(session: Session): StartRoute {
  return session.status === 'new' ? ROUTES.welcome : ROUTES.home
}
