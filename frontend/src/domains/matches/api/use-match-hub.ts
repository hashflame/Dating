import { skipToken, useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type MatchHub } from '../types/match'

import { matchKeys } from './match-keys'

/** Хаб мэтча (S-31). `undefined` — мэтч не выбран, запрос не уходит. */
export function useMatchHub(matchId: string | undefined): UseQueryResult<MatchHub, Error> {
  return useQuery({
    queryKey: matchKeys.hub(matchId ?? ''),
    queryFn:
      matchId === undefined
        ? skipToken
        : ({ signal }) => apiRequest<MatchHub>(`/api/matches/${matchId}`, { signal }),
    staleTime: 30 * 1000,
  })
}
