import { skipToken, useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type QuestionOfDay } from '../types/match'

import { matchKeys } from './match-keys'

/** Вопрос дня для мэтча (S-37). Новый публикуется раз в сутки в 19:00. */
export function useQuestionOfDay(
  matchId: string | undefined,
): UseQueryResult<QuestionOfDay, Error> {
  return useQuery({
    queryKey: matchKeys.question(matchId ?? ''),
    queryFn:
      matchId === undefined
        ? skipToken
        : ({ signal }) =>
            apiRequest<QuestionOfDay>(`/api/matches/${matchId}/question-of-day`, { signal }),
    staleTime: 60 * 1000,
  })
}
