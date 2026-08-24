import { skipToken, useQuery, type UseQueryResult } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'

import { apiRequest } from '@/shared/api'

import { type City } from '../types/city'

import { cityKeys } from './city-keys'

/**
 * Город по id — нужен, чтобы показать сохранённый в черновике выбор:
 * там лежит только `cityId`, а на экране нужно название.
 */
export function useCity(cityId: string | undefined): UseQueryResult<City, Error> {
  const { i18n } = useTranslation()
  const locale = i18n.language

  return useQuery({
    queryKey: cityKeys.byId(cityId ?? '', locale),
    queryFn: cityId
      ? ({ signal }) => apiRequest<City>(`/api/cities/${cityId}`, { query: { locale }, signal })
      : skipToken,
    staleTime: 5 * 60 * 1000,
  })
}
