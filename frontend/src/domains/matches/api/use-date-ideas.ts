import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type DateIdea } from '../types/match'

import { matchKeys } from './match-keys'

type DateIdeas = {
  ideas: DateIdea[]
}

/**
 * Идеи свидания для мэтча (T-12.1, S-39): от 0 до 3 вариантов по пересечению
 * «Предпочтений на свидания» обоих участников.
 *
 * Фильтры `city`/`maxBudget`/`currency` сервер принимает, но пока не отправляем:
 * город он берёт сам, а бюджетом на макете (S-39) управляют чипсы, которых в
 * MVP-каталоге нечем наполнить — вариантов всего три.
 */
export function useDateIdeas(matchId: string): UseQueryResult<DateIdeas, Error> {
  return useQuery({
    queryKey: matchKeys.dateIdeas(matchId),
    queryFn: ({ signal }) =>
      apiRequest<DateIdeas>(`/api/matches/${matchId}/date-ideas`, { signal }),
    staleTime: 5 * 60 * 1000,
  })
}
