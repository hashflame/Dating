import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { track } from '@/shared/analytics'
import { apiRequest, isApiError } from '@/shared/api'

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
    onSuccess: (photo) => {
      track({ name: 'photo_uploaded', source: 'telegram', index: photo.sortOrder })

      return queryClient.invalidateQueries({ queryKey: photoKeys.list() })
    },
    onError: (error) =>
      track({
        name: 'photo_upload_failed',
        source: 'telegram',
        reason: isApiError(error) ? error.code : 'UNKNOWN',
      }),
  })
}
