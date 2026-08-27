import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type SwipeAction, type SwipeResult } from '../types/feed'

import { feedKeys } from './feed-keys'

type SwipeInput = {
  userId: string
  action: SwipeAction
}

/**
 * Лайк или дизлайк. Карточку из кэша убираем сразу, не дожидаясь
 * ответа: иначе между нажатием и следующей анкетой видна пауза. Список
 * перезапрашиваем только когда он опустел.
 */
export function useSwipe(): UseMutationResult<SwipeResult, Error, SwipeInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ userId, action }) =>
      apiRequest<SwipeResult>(`/api/feed/${userId}/${action}`, { method: 'POST' }),
    onSettled: () => queryClient.invalidateQueries({ queryKey: feedKeys.cards() }),
  })
}
