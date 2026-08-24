import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type FeedFilters } from '../types/feed'

import { feedKeys } from './feed-keys'

/**
 * Сохраняет фильтры. Отправляем весь набор: PATCH принимает частичный объект,
 * но экран правит всё сразу, и полное тело избавляет от догадок, что изменилось.
 *
 * Ответ кладём в кэш, а ленту инвалидируем — подбор считается серверно.
 */
export function useSaveFeedFilters(): UseMutationResult<FeedFilters, Error, FeedFilters> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (filters) =>
      apiRequest<FeedFilters>('/api/feed/filters', { method: 'PATCH', body: filters }),
    onSuccess: (filters) => {
      queryClient.setQueryData(feedKeys.filters(), filters)
      void queryClient.invalidateQueries({ queryKey: feedKeys.cards() })
    },
  })
}
