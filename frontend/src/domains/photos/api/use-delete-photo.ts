import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { photoKeys } from './photo-keys'

/** Удаляет фото. Если оно было главным, бэкенд назначает главным следующее. */
export function useDeletePhoto(): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (photoId) =>
      apiRequest<void>(`/api/users/me/photos/${photoId}`, { method: 'DELETE' }),
    // Главное фото могло переехать на другое — список перечитываем целиком.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: photoKeys.list() }),
  })
}
