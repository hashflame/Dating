import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { viewerKeys } from './viewer-keys'

/**
 * Снимает аккаунт с паузы (T-16.2, S-51). На аккаунт в любом другом статусе
 * сервер не действует: удалённый или забаненный через resume не воскресает.
 */
export function useResumeAccount(): UseMutationResult<void, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => apiRequest<void>('/api/users/me/resume', { method: 'POST' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: viewerKeys.root }),
  })
}
