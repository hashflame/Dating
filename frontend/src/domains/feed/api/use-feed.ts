import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { useEffect, useRef } from 'react'

import { track } from '@/shared/analytics'
import { apiRequest } from '@/shared/api'

import { swipesInSession } from '../lib/session-swipes'
import { type Feed } from '../types/feed'

import { feedKeys } from './feed-keys'

/** Сколько карточек просим за раз: дек показывает одну, запас нужен для «следующей». */
const FEED_PAGE_SIZE = 10

/**
 * Подобранные анкеты. Кэш не устаревает сам: набор меняется только нашими
 * свайпами и фильтрами, и оба случая инвалидируют его явно.
 */
export function useFeed(): UseQueryResult<Feed, Error> {
  const query = useQuery({
    queryKey: feedKeys.cards(),
    queryFn: ({ signal }) =>
      apiRequest<Feed>('/api/feed', { query: { limit: FEED_PAGE_SIZE }, signal }),
    staleTime: Infinity,
  })

  // Флаг сервера и пустой список — одно и то же для человека: показывать
  // нечего. Считаем оба, иначе метрика зависела бы от того, успел ли бэкенд
  // выставить `exhausted`.
  useExhaustedEvent(
    query.data !== undefined && (query.data.exhausted || query.data.items.length === 0),
  )

  return query
}

/**
 * `feed_exhausted` — один раз на переход в пустую ленту, а не на каждый рендер.
 * Когда карточки снова появились, следующий конец опять считается новым.
 */
function useExhaustedEvent(exhausted: boolean): void {
  const sent = useRef(false)

  useEffect(() => {
    if (!exhausted) {
      sent.current = false
      return
    }

    if (sent.current) return
    sent.current = true

    track({ name: 'feed_exhausted', swipes_in_session: swipesInSession() })
  }, [exhausted])
}
