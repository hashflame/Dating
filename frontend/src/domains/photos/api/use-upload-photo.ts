import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { viewerKeys } from '@/domains/viewer'
import { track } from '@/shared/analytics'
import { apiRequest, isApiError } from '@/shared/api'

import { type Photo } from '../types/photo'

import { photoKeys } from './photo-keys'

/** Загружает файл фото. Бэкенд снимает EXIF и отвечает 422 на превышение лимита. */
export function useUploadPhoto(): UseMutationResult<Photo, Error, File> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (file) => {
      const formData = new FormData()
      formData.append('file', file)

      return apiRequest<Photo>('/api/users/me/photos', { method: 'POST', formData })
    },
    // Сервер сам расставляет sortOrder и делает первое фото главным,
    // поэтому список перечитываем, а не досоставляем в кэше.
    onSuccess: (photo) => {
      track({ name: 'photo_uploaded', source: 'file', index: photo.sortOrder })
      void queryClient.invalidateQueries({ queryKey: photoKeys.list() })
      // Профиль отдаёт и сами фото, и заполненность карточки — без этого
      // процент и счётчик фото оставались вчерашними до перезапуска.
      void queryClient.invalidateQueries({ queryKey: viewerKeys.root })
    },
    // Код ошибки, не текст: тексты сервер локализует и меняет, коды — нет.
    onError: (error) =>
      track({
        name: 'photo_upload_failed',
        source: 'file',
        reason: isApiError(error) ? error.code : 'UNKNOWN',
      }),
  })
}
