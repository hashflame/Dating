import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type UnlockedContact } from '../types/match'

import { matchKeys } from './match-keys'

/**
 * Открывает контакт мэтча — платно, списывает зорки.
 *
 * Ответ 402 означает «не хватает зорок»: показываем это отдельно, а не общей
 * ошибкой. Баланс после списания приходит в ответе, но профиль всё равно
 * перечитываем — он источник правды для остальных экранов.
 */
export function useUnlockContact(): UseMutationResult<UnlockedContact, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (matchId) =>
      apiRequest<UnlockedContact>(`/api/matches/${matchId}/unlock`, { method: 'POST' }),
    onSuccess: (_data, matchId) => {
      void queryClient.invalidateQueries({ queryKey: ['viewer'] })
      // Меняется contactStatus в хабе и секция в списке мэтчей.
      void queryClient.invalidateQueries({ queryKey: matchKeys.hub(matchId) })
      void queryClient.invalidateQueries({ queryKey: matchKeys.list() })
    },
  })
}
