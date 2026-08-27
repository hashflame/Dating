import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { feedKeys } from '@/domains/feed'
import { likeKeys } from '@/domains/likes'
import { matchKeys } from '@/domains/matches'
import { apiRequest } from '@/shared/api'

import { moderationKeys } from './moderation-keys'

/**
 * Блокировка пользователя (T-16.2, S-11). Блокировка двусторонняя: он уходит из
 * ленты, симпатий и мэтчей — и мы из его. Поэтому перезапрашиваем всё сразу.
 * Идемпотентна: повторный вызов тоже отдаёт 204.
 */
export function useBlockUser(): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (userId) => apiRequest<void>(`/api/users/${userId}/block`, { method: 'POST' }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: feedKeys.root })
      void queryClient.invalidateQueries({ queryKey: likeKeys.root })
      void queryClient.invalidateQueries({ queryKey: matchKeys.root })
      void queryClient.invalidateQueries({ queryKey: moderationKeys.root })
    },
  })
}
