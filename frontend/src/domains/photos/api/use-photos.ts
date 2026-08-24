import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type Photo } from '../types/photo'

import { photoKeys } from './photo-keys'

/** Список фото профиля, отсортированный бэкендом по `sortOrder`. */
export function usePhotos(): UseQueryResult<Photo[], Error> {
  return useQuery({
    queryKey: photoKeys.list(),
    queryFn: ({ signal }) => apiRequest<Photo[]>('/api/users/me/photos', { signal }),
    staleTime: 60 * 1000,
  })
}
