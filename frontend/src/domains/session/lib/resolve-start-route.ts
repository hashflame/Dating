import { ROUTES } from '@/shared/config'

import { type Session, type UserStatus } from '../types/session'

/**
 * Статусы, при которых пользователь ещё проходит анкету. В `onboarding`
 * бэкенд переводит его при сохранении первого шага черновика.
 */
const ONBOARDING_STATUSES: readonly UserStatus[] = ['new', 'onboarding']

/** Нужны ли этой сессии экраны онбординга. */
export function isOnboardingSession(session: Session): boolean {
  return ONBOARDING_STATUSES.includes(session.status)
}

/** Экран шага по числу уже заполненных шагов анкеты. */
const ONBOARDING_ROUTE_BY_STEP = [
  ROUTES.onboardingAbout,
  ROUTES.onboardingPreferences,
  ROUTES.onboardingCity,
  ROUTES.onboardingPhotos,
] as const

type StartRoute =
  | typeof ROUTES.welcome
  | typeof ROUTES.feed
  | typeof ROUTES.onboardingDone
  | (typeof ONBOARDING_ROUTE_BY_STEP)[number]

type StartRouteInput = {
  session: Session
  /** Согласие текущей версии получено (`GET /api/users/me/consent`). */
  consentGiven: boolean
  /** Число заполненных шагов анкеты (`draft.step`). */
  completedSteps: number
}

/**
 * Куда вести после проверки сессии.
 *
 * Анкету проходят статусы `new` и `onboarding`: во второй бэкенд переводит
 * пользователя при сохранении первого шага. Всех остальных ведём в ленту.
 *
 * Без согласия показываем приветствие: бэкенд всё равно не даст завершить
 * онбординг. Дальше возвращаем на первый незаполненный шаг, чтобы не проходить
 * анкету заново.
 */
export function resolveStartRoute({
  session,
  consentGiven,
  completedSteps,
}: StartRouteInput): StartRoute {
  if (!isOnboardingSession(session)) return ROUTES.feed
  if (!consentGiven) return ROUTES.welcome

  return ONBOARDING_ROUTE_BY_STEP[completedSteps] ?? ROUTES.onboardingDone
}
