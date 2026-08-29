import { skipToken, useQuery, type UseQueryResult } from '@tanstack/react-query'
import { useEffect, useRef } from 'react'

import { track } from '@/shared/analytics'
import { apiRequest } from '@/shared/api'

import { type MatchHub } from '../types/match'

import { matchKeys } from './match-keys'

/** Хаб мэтча (S-31). `undefined` — мэтч не выбран, запрос не уходит. */
export function useMatchHub(matchId: string | undefined): UseQueryResult<MatchHub, Error> {
  const query = useQuery({
    queryKey: matchKeys.hub(matchId ?? ''),
    queryFn:
      matchId === undefined
        ? skipToken
        : ({ signal }) => apiRequest<MatchHub>(`/api/matches/${matchId}`, { signal }),
    staleTime: 30 * 1000,
  })

  useHubOpenedEvent(query.isSuccess ? matchId : undefined)

  return query
}

/**
 * `match_hub_opened` — один раз на мэтч, а не на каждый рендер и не на
 * повторное чтение из кэша. Id мэтча в событие не уходит: он нужен только
 * чтобы отличить один открытый хаб от другого.
 */
function useHubOpenedEvent(matchId: string | undefined): void {
  const sent = useRef<string | null>(null)

  useEffect(() => {
    if (matchId === undefined || sent.current === matchId) return

    sent.current = matchId
    track({ name: 'match_hub_opened' })
  }, [matchId])
}
