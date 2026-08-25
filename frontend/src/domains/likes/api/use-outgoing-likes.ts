import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type OutgoingLikes } from '../types/like'

import { likeKeys } from './like-keys'

/**
 * Кому лайк поставили мы. Платить за просмотр не нужно — это наши же действия.
 * Запрашиваем сразу, не по открытию вкладки: число стоит в её подписи.
 */
export function useOutgoingLikes(): UseQueryResult<OutgoingLikes, Error> {
  return useQuery({
    queryKey: likeKeys.outgoing(),
    queryFn: ({ signal }) => apiRequest<OutgoingLikes>('/api/likes/outgoing', { signal }),
    staleTime: 60 * 1000,
  })
}
