import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type BlockedUser } from '../types/moderation'

import { moderationKeys } from './moderation-keys'

/** Кого я заблокировал (T-16.2, S-51) — от свежих блокировок к старым. */
export function useBlockedUsers(): UseQueryResult<BlockedUser[], Error> {
  return useQuery({
    queryKey: moderationKeys.blocked(),
    queryFn: ({ signal }) => apiRequest<BlockedUser[]>('/api/users/me/blocked', { signal }),
  })
}
