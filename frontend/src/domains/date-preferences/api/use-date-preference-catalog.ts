import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'

import { apiRequest } from '@/shared/api'

import { type DatePreference } from '../types/date-preference'

/** Каталог предпочтений на свидания (S-42). Их всего четыре, меняются редко. */
export function useDatePreferenceCatalog(): UseQueryResult<DatePreference[], Error> {
  const { i18n } = useTranslation()
  const locale = i18n.language

  return useQuery({
    queryKey: ['date-preferences', 'catalog', locale],
    queryFn: ({ signal }) =>
      apiRequest<DatePreference[]>('/api/date-preferences/catalog', {
        query: { locale },
        signal,
      }),
    staleTime: 30 * 60 * 1000,
  })
}
