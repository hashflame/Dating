import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { likeKeys } from '@/domains/likes'
import { matchKeys } from '@/domains/matches'
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
 *
 * Свайп меняет не только ленту: лайк уходит в «Ваши лайки», а взаимный — ещё и
 * в мэтчи. Без их инвалидации человек лайкал из ленты, открывал симпатии и не
 * находил там только что лайкнутого — список оставался прежним до перезапуска.
 */
export function useSwipe(): UseMutationResult<SwipeResult, Error, SwipeInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ userId, action }) =>
      apiRequest<SwipeResult>(`/api/feed/${userId}/${action}`, { method: 'POST' }),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: feedKeys.cards() })
      void queryClient.invalidateQueries({ queryKey: likeKeys.root })
      void queryClient.invalidateQueries({ queryKey: matchKeys.root })
    },
  })
}
