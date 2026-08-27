import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { matchKeys } from './match-keys'

/**
 * «Мы договорились о встрече» (T-12.1, S-39) — главный сигнал качества для
 * алгоритма. Идемпотентно: повторный вызов не сдвигает отметку.
 */
export function useConfirmDate(): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (matchId) =>
      apiRequest<void>(`/api/matches/${matchId}/date-confirmed`, { method: 'POST' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: matchKeys.root }),
  })
}
