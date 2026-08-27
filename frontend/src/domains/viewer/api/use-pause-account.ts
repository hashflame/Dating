import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { viewerKeys } from './viewer-keys'

/**
 * Пауза аккаунта (T-16.2, S-51): `Status = Paused`, анкета исчезает из ленты,
 * мэтчи и переписки сохраняются. Снимается `useResumeAccount`. Идемпотентно.
 */
export function usePauseAccount(): UseMutationResult<void, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => apiRequest<void>('/api/users/me/pause', { method: 'POST' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: viewerKeys.root }),
  })
}
