import { skipToken, useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type UserProfile } from '../types/profile'

/**
 * Анкета другого пользователя. Нужна там, где полной карточки нет под рукой:
 * в симпатиях список отдаёт только имя, возраст и фото.
 * `undefined` — никого не открыли, запрос не уходит.
 */
export function useUserProfile(userId: string | undefined): UseQueryResult<UserProfile, Error> {
  return useQuery({
    queryKey: ['profiles', userId],
    queryFn:
      userId === undefined
        ? skipToken
        : ({ signal }) => apiRequest<UserProfile>(`/api/users/${userId}`, { signal }),
    staleTime: 5 * 60 * 1000,
  })
}
