import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { type Viewer } from '../types/viewer'

import { getViewer } from './get-viewer'
import { viewerKeys } from './viewer-keys'

/** Профиль текущего пользователя. Кэшируется на всё время сессии. */
export function useViewer(): UseQueryResult<Viewer, Error> {
  return useQuery({
    queryKey: viewerKeys.me(),
    queryFn: () => getViewer(),
    staleTime: 5 * 60 * 1000,
  })
}
