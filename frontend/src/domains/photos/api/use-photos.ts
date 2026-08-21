import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { stub } from '@/shared/api'

import { type Photo } from '../types/photo'

import { photoKeys } from './photo-keys'

/** Список фото профиля. */
export function usePhotos(): UseQueryResult<Photo[], Error> {
  return useQuery({
    queryKey: photoKeys.list(),
    // @stub: GET /api/users/me/photos на бэкенде нет — см. docs/api-gaps.md
    queryFn: () => stub<Photo[]>('GET /api/users/me/photos', []),
    staleTime: Infinity,
  })
}
