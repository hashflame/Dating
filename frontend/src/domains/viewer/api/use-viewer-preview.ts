import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type ViewerPreview } from '../types/viewer'

import { viewerKeys } from './viewer-keys'

/**
 * Своя анкета глазами других (S-40). Отдельный запрос, а не сборка из
 * `useViewer`: сервер сам считает возраст, название города и главное фото —
 * повторять эту логику на клиенте значит расходиться с лентой.
 */
export function useViewerPreview(enabled: boolean): UseQueryResult<ViewerPreview, Error> {
  return useQuery({
    queryKey: viewerKeys.preview(),
    queryFn: ({ signal }) => apiRequest<ViewerPreview>('/api/users/me/preview', { signal }),
    staleTime: 60 * 1000,
    enabled,
  })
}
