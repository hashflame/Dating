import { ROUTES } from '@/shared/config'

import { type Session } from '../types/session'

/** Шаг анкеты, на котором пользователь остановился, по числу заполненных. */
const ONBOARDING_ROUTE_BY_STEP = [
  ROUTES.onboardingAbout,
  ROUTES.onboardingPreferences,
  ROUTES.onboardingCity,
  ROUTES.onboardingPhotos,
] as const

type StartRoute =
  | typeof ROUTES.welcome
  | typeof ROUTES.home
  | typeof ROUTES.onboardingDone
  | (typeof ONBOARDING_ROUTE_BY_STEP)[number]

/**
 * Куда вести после проверки сессии.
 *
 * Незаполненный черновик — значит человек здесь впервые: показываем приветствие
 * и согласие. Начатый — возвращаем на тот шаг, где он остановился, чтобы не
 * проходить анкету заново. Отдельного признака «согласие получено» в API нет,
 * поэтому его роль играет первый заполненный шаг — см. docs/api-gaps.md.
 *
 * @param completedSteps число заполненных шагов анкеты (`draft.step`).
 */
export function resolveStartRoute(session: Session, completedSteps: number): StartRoute {
  if (session.status !== 'new') return ROUTES.home
  if (completedSteps <= 0) return ROUTES.welcome

  return ONBOARDING_ROUTE_BY_STEP[completedSteps] ?? ROUTES.onboardingDone
}
