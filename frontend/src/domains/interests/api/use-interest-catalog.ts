import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'

import { apiRequest } from '@/shared/api'

import { type InterestGroup } from '../types/interest'

import { interestKeys } from './interest-keys'

/** Каталог интересов, сгруппированный по категориям. Меняется редко — держим долго. */
export function useInterestCatalog(): UseQueryResult<InterestGroup[], Error> {
  const { i18n } = useTranslation()
  const locale = i18n.language

  return useQuery({
    queryKey: interestKeys.catalog(locale),
    queryFn: ({ signal }) =>
      apiRequest<InterestGroup[]>('/api/interests/catalog', { query: { locale }, signal }),
    staleTime: 30 * 60 * 1000,
  })
}
