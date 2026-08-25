import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { stub } from '@/shared/api'

/**
 * Блокировка пользователя (S-11, спека §20). Заблокированный уходит из ленты,
 * поэтому после успеха её перезапрашиваем.
 */
export function useBlockUser(): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    // @stub: POST /api/users/{userId}/block на бэкенде нет — см. docs/api-gaps.md
    mutationFn: (userId) => stub<void>(`POST /api/users/${userId}/block`, undefined),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['feed'] }),
  })
}
