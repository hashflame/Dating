import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

type SaveInterestsInput = {
  /** Id из каталога. */
  interestIds: string[]
  /** Названия, которых в каталоге нет — сервер создаёт их сам. */
  customInterests: string[]
}

/**
 * Задаёт интересы целиком: сервер заменяет прежний набор присланным.
 * Ответ содержит бонус за впервые достигнутый порог заполненности, поэтому
 * обновляем профиль — там баланс зорок.
 */
export function useSaveInterests(): UseMutationResult<unknown, Error, SaveInterestsInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body) => apiRequest<unknown>('/api/users/me/interests', { method: 'PATCH', body }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['viewer'] }),
  })
}
