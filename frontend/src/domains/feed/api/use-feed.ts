import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type Feed } from '../types/feed'

import { feedKeys } from './feed-keys'

/** Сколько карточек просим за раз: дек показывает одну, запас нужен для «следующей». */
const FEED_PAGE_SIZE = 10

/**
 * Подобранные анкеты. Кэш не устаревает сам: набор меняется только нашими
 * свайпами и фильтрами, и оба случая инвалидируют его явно.
 */
export function useFeed(): UseQueryResult<Feed, Error> {
  return useQuery({
    queryKey: feedKeys.cards(),
    queryFn: ({ signal }) =>
      apiRequest<Feed>('/api/feed', { query: { limit: FEED_PAGE_SIZE }, signal }),
    staleTime: Infinity,
  })
}
