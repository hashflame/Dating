import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

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
    // Список приходит из заглушки, поэтому добавляем результат в кэш руками.
    onSuccess: (photo) =>
      queryClient.setQueryData<Photo[]>(photoKeys.list(), (photos = []) => [...photos, photo]),
  })
}
