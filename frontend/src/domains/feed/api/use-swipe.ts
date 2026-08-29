import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { likeKeys } from '@/domains/likes'
import { matchKeys } from '@/domains/matches'
import { track } from '@/shared/analytics'
import { apiRequest } from '@/shared/api'

import { countSwipe, swipesInSession } from '../lib/session-swipes'
import { type SwipeAction, type SwipeResult } from '../types/feed'

import { feedKeys } from './feed-keys'

type SwipeInput = {
  userId: string
  action: SwipeAction
  /**
   * Откуда решение: дека ленты или ответ на входящую симпатию. На сервере это
   * один и тот же запрос, а в аналитике — разные места продукта.
   */
  source: 'feed' | 'likes' | 'matches'
  /**
   * Сколько секунд карточка была на экране. Знает это только экран, а событие
   * живёт здесь — рядом с самим действием, чтобы не потеряться при переверстке
   * ленты. Поэтому время и приходит входом мутации. Вне ленты не измеряется.
   */
  secondsOnCard?: number
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
    onSuccess: (result, { source, secondsOnCard }) => {
      // Порядковый номер карточки в сессии — по нему видно, на каком свайпе
      // люди перестают листать. Считаем до инкремента: первая карточка — 1.
      // Ответы на входящие симпатии в счёт не идут: это не листание ленты.
      const position = swipesInSession() + 1
      if (source === 'feed') countSwipe()

      track({
        name: 'swipe',
        source,
        action: result.action,
        position,
        seconds_on_card: secondsOnCard ?? null,
        is_match: result.isMatch,
      })
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: feedKeys.cards() })
      void queryClient.invalidateQueries({ queryKey: likeKeys.root })
      void queryClient.invalidateQueries({ queryKey: matchKeys.root })
    },
  })
}
