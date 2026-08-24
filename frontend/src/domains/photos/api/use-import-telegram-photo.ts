import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type Photo } from '../types/photo'

import { photoKeys } from './photo-keys'

/**
 * Импортирует аватар из Telegram. Сервер его не хранит, поэтому URL присылает клиент:
 * берётся из launch params (`getTelegramUser`).
 */
export function useImportTelegramPhoto(): UseMutationResult<Photo, Error, string> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (photoUrl) =>
      apiRequest<Photo>('/api/users/me/photos/import-telegram', {
        method: 'POST',
        body: { photoUrl },
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: photoKeys.list() }),
  })
}
