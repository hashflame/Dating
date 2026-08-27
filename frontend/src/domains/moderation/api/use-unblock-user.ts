import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { feedKeys } from '@/domains/feed'
import { apiRequest } from '@/shared/api'

import { moderationKeys } from './moderation-keys'

/** Снятие блокировки из списка «Заблокированные» (T-16.2, S-51). Идемпотентно. */
export function useUnblockUser(): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (userId) => apiRequest<void>(`/api/users/${userId}/block`, { method: 'DELETE' }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: moderationKeys.root })
      void queryClient.invalidateQueries({ queryKey: feedKeys.root })
    },
  })
}
