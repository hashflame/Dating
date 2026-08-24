import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type FeedFilters } from '../types/feed'

import { feedKeys } from './feed-keys'

/** Текущие фильтры подбора. Дефолты при регистрации заводит бэкенд из шага 2 анкеты. */
export function useFeedFilters(enabled = true): UseQueryResult<FeedFilters, Error> {
  return useQuery({
    queryKey: feedKeys.filters(),
    queryFn: ({ signal }) => apiRequest<FeedFilters>('/api/feed/filters', { signal }),
    staleTime: 5 * 60 * 1000,
    enabled,
  })
}
