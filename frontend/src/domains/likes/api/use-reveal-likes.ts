import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { track } from '@/shared/analytics'
import { apiRequest } from '@/shared/api'

import { type RevealedLikes } from '../types/like'

import { likeKeys } from './like-keys'

/**
 * Раскрывает входящие симпатии за зорки — навсегда, а не по одному человеку.
 * Повторный вызов бесплатен (`sparksSpent: 0`), поэтому двойное нажатие не страшно.
 */
export function useRevealLikes(): UseMutationResult<RevealedLikes, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => apiRequest<RevealedLikes>('/api/likes/incoming/reveal', { method: 'POST' }),
    onSuccess: (result) => {
      // 0 — раскрытие уже было оплачено раньше: повторный вызов бесплатен.
      track({ name: 'likes_revealed', cost: result.sparksSpent })

      // Меняется и список, и баланс зорок в шапке.
      void queryClient.invalidateQueries({ queryKey: likeKeys.incoming() })
      void queryClient.invalidateQueries({ queryKey: ['viewer'] })
    },
  })
}
