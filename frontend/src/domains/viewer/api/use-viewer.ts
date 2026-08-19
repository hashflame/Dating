import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { stub } from '@/shared/api'

import { type Viewer } from '../types/viewer'

const viewerKeys = {
  root: ['viewer'] as const,
  me: () => [...viewerKeys.root, 'me'] as const,
}

const VIEWER_FIXTURE: Viewer = {
  id: '00000000-0000-0000-0000-000000000001',
  telegramId: 99281932,
  firstName: 'Дзмітры',
  balance: 25,
  isOnboarded: false,
}

function getViewer(): Promise<Viewer> {
  // @stub: GET /api/users/me на бэкенде нет — см. docs/api-gaps.md
  return stub('GET /api/users/me', VIEWER_FIXTURE)
}

export function useViewer(): UseQueryResult<Viewer, Error> {
  return useQuery({
    queryKey: viewerKeys.me(),
    queryFn: getViewer,
    staleTime: 5 * 60 * 1000,
  })
}
