import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'
import { CONSENT_VERSION } from '@/shared/config'

import { type UserConsentStatus } from '../types/consent'

import { onboardingKeys } from './onboarding-keys'

/**
 * Дано ли согласие текущей версии.
 *
 * @param enabled запрос нужен только на старте нового пользователя: тех, кто
 * прошёл анкету, экран загрузки ведёт в ленту, и лишний запрос задержал бы старт.
 */
export function useConsentGiven(enabled = true): UseQueryResult<boolean, Error> {
  return useQuery({
    queryKey: onboardingKeys.consent(),
    queryFn: ({ signal }) => apiRequest<UserConsentStatus[]>('/api/users/me/consent', { signal }),
    // Согласие прошлой версии не считается: текст изменился, нужно новое.
    select: (statuses) =>
      statuses.some(
        (status) =>
          status.type === 'termsAndPrivacyPolicy' &&
          status.given &&
          status.version === CONSENT_VERSION,
      ),
    staleTime: 60 * 1000,
    enabled,
  })
}
