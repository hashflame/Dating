import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { viewerKeys } from '@/domains/viewer'
import { apiRequest } from '@/shared/api'

import { type Photo } from '../types/photo'

import { photoKeys } from './photo-keys'

type ReorderInput = {
  /** Все id фото пользователя в новом порядке — бэкенд требует полный набор. */
  order: string[]
  mainPhotoId: string
}

/** Меняет порядок фото и назначает главное. */
export function useReorderPhotos(): UseMutationResult<Photo[], Error, ReorderInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input) =>
      apiRequest<Photo[]>('/api/users/me/photos/reorder', { method: 'PATCH', body: input }),
    onSuccess: (photos) => {
      queryClient.setQueryData(photoKeys.list(), photos)
      void queryClient.invalidateQueries({ queryKey: viewerKeys.root })
    },
  })
}
