import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type Viewer } from '../types/viewer'

import { viewerKeys } from './viewer-keys'

/** Профиль текущего пользователя: имя, статус, баланс зорок, заполненность. */
export function useViewer(): UseQueryResult<Viewer, Error> {
  return useQuery({
    queryKey: viewerKeys.me(),
    queryFn: ({ signal }) => apiRequest<Viewer>('/api/users/me', { signal }),
    staleTime: 5 * 60 * 1000,
  })
}
