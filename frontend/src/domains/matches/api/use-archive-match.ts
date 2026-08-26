import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { matchKeys } from './match-keys'

type ArchiveInput = {
  matchId: string
  /** `false` — вернуть из архива. Возврат бесплатный и доступен всегда (S-30). */
  archived: boolean
}

/** Убирает мэтч в архив или возвращает обратно. */
export function useArchiveMatch(): UseMutationResult<void, Error, ArchiveInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ matchId, archived }) =>
      apiRequest<void>(`/api/matches/${matchId}/archive`, {
        method: archived ? 'POST' : 'DELETE',
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: matchKeys.root }),
  })
}
