import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type OnboardingDraftState } from '../types/onboarding'

import { onboardingKeys } from './onboarding-keys'

/**
 * Черновик анкеты: позволяет вернуться на тот шаг, где пользователь остановился.
 *
 * @param enabled экрану загрузки черновик нужен только новому пользователю —
 * остальных он ведёт в ленту, и лишний запрос там задерживал бы старт.
 */
export function useOnboardingDraft(enabled = true): UseQueryResult<OnboardingDraftState, Error> {
  return useQuery({
    queryKey: onboardingKeys.draft(),
    queryFn: ({ signal }) => apiRequest<OnboardingDraftState>('/api/onboarding/draft', { signal }),
    staleTime: 60 * 1000,
    enabled,
  })
}
