import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type IncomingLikes } from '../types/like'

const likeKeys = {
  root: ['likes'] as const,
  incoming: () => [...likeKeys.root, 'incoming'] as const,
}

/** Входящие симпатии. Нижнему меню нужно только число для бейджа. */
export function useIncomingLikes(): UseQueryResult<IncomingLikes, Error> {
  return useQuery({
    queryKey: likeKeys.incoming(),
    queryFn: ({ signal }) => apiRequest<IncomingLikes>('/api/likes/incoming', { signal }),
    staleTime: 60 * 1000,
  })
}
