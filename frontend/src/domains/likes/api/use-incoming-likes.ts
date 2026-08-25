import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type IncomingLikes } from '../types/like'

import { likeKeys } from './like-keys'

/**
 * Входящие симпатии. Нижнему меню нужно только число для бейджа, экрану —
 * ещё заблюренные превью или полный список, смотря оплачено ли раскрытие.
 */
export function useIncomingLikes(): UseQueryResult<IncomingLikes, Error> {
  return useQuery({
    queryKey: likeKeys.incoming(),
    queryFn: ({ signal }) => apiRequest<IncomingLikes>('/api/likes/incoming', { signal }),
    staleTime: 60 * 1000,
  })
}
