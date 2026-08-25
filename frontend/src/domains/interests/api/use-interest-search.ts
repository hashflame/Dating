import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'

import { apiRequest } from '@/shared/api'

import { type Interest } from '../types/interest'

import { interestKeys } from './interest-keys'

/** Поиск по каталогу. Пустой запрос не отправляем — каталог и так показан целиком. */
export function useInterestSearch(query: string): UseQueryResult<Interest[], Error> {
  const { i18n } = useTranslation()
  const locale = i18n.language

  return useQuery({
    queryKey: interestKeys.search(query, locale),
    queryFn: ({ signal }) =>
      apiRequest<Interest[]>('/api/interests/search', { query: { q: query, locale }, signal }),
    enabled: query.trim().length > 0,
    staleTime: 5 * 60 * 1000,
  })
}
