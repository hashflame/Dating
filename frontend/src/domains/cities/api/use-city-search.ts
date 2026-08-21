import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'

import { apiRequest } from '@/shared/api'

import { type City } from '../types/city'

const cityKeys = {
  root: ['cities'] as const,
  search: (query: string, locale: string) => [...cityKeys.root, 'search', query, locale] as const,
}

/** Поиск городов каталога по подстроке. Запрос уходит только с непустым `query`. */
export function useCitySearch(query: string): UseQueryResult<City[], Error> {
  const { i18n } = useTranslation()
  const locale = i18n.language

  return useQuery({
    queryKey: cityKeys.search(query, locale),
    queryFn: ({ signal }) =>
      apiRequest<City[]>('/api/cities/search', { query: { q: query, locale }, signal }),
    enabled: query.trim().length > 0,
    staleTime: 5 * 60 * 1000,
  })
}
