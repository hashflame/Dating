import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { viewerKeys } from '@/domains/viewer'
import { apiRequest } from '@/shared/api'

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
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: photoKeys.list() })
      // Профиль отдаёт и сами фото, и заполненность карточки — без этого
      // процент и счётчик фото оставались вчерашними до перезапуска.
      void queryClient.invalidateQueries({ queryKey: viewerKeys.root })
    },
  })
}
