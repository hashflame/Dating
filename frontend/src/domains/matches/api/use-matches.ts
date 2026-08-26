import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type Matches } from '../types/match'

import { matchKeys } from './match-keys'

/** Список мэтчей тремя секциями (S-30): новые, ждут сообщения, архив. */
export function useMatches(): UseQueryResult<Matches, Error> {
  return useQuery({
    queryKey: matchKeys.list(),
    queryFn: ({ signal }) => apiRequest<Matches>('/api/matches', { signal }),
    staleTime: 60 * 1000,
  })
}
