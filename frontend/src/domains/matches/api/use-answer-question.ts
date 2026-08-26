import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { matchKeys } from './match-keys'

type AnswerInput = {
  matchId: string
  text: string
}

/**
 * Отвечает на вопрос дня. Ответ собеседника открывается только когда ответили
 * оба, поэтому после отправки перечитываем вопрос целиком.
 */
export function useAnswerQuestion(): UseMutationResult<unknown, Error, AnswerInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ matchId, text }) =>
      apiRequest<unknown>(`/api/matches/${matchId}/question-of-day/answer`, {
        method: 'POST',
        body: { text },
      }),
    onSuccess: (_data, { matchId }) => {
      void queryClient.invalidateQueries({ queryKey: matchKeys.question(matchId) })
      void queryClient.invalidateQueries({ queryKey: matchKeys.questionsArchive(matchId) })
    },
  })
}
