import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type PrivacySettings } from '../types/privacy'

import { privacyKeys } from './privacy-keys'

/** Текущие настройки приватности (T-16.1, S-51). */
export function usePrivacySettings(): UseQueryResult<PrivacySettings, Error> {
  return useQuery({
    queryKey: privacyKeys.settings(),
    queryFn: ({ signal }) => apiRequest<PrivacySettings>('/api/privacy/settings', { signal }),
    staleTime: 5 * 60 * 1000,
  })
}
