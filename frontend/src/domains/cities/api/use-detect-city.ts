import { useMutation, type UseMutationResult } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'

import { apiRequest } from '@/shared/api'

import { type DetectedCity } from '../types/city'

type Coordinates = { lat: number; lon: number }

/** Обратное геокодирование координат в город каталога. */
export function useDetectCity(): UseMutationResult<DetectedCity, Error, Coordinates> {
  const { i18n } = useTranslation()
  const locale = i18n.language

  return useMutation({
    mutationFn: (coordinates) =>
      apiRequest<DetectedCity>('/api/geo/detect', {
        method: 'POST',
        body: coordinates,
        query: { locale },
      }),
  })
}
