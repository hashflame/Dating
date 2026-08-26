import { skipToken, useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type QuestionArchiveItem } from '../types/match'

import { matchKeys } from './match-keys'

type Archive = {
  items: QuestionArchiveItem[]
  page: number
  pageSize: number
  totalCount: number
  hasMore: boolean
}

/** Прошлые вопросы и ответы (S-37). Берём первую страницу. */
export function useQuestionsArchive(matchId: string | undefined): UseQueryResult<Archive, Error> {
  return useQuery({
    queryKey: matchKeys.questionsArchive(matchId ?? ''),
    queryFn:
      matchId === undefined
        ? skipToken
        : ({ signal }) =>
            apiRequest<Archive>(`/api/matches/${matchId}/questions/archive`, { signal }),
    staleTime: 5 * 60 * 1000,
  })
}
