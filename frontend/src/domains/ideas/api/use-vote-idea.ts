import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type Idea } from '../types/idea'

import { ideaKeys } from './idea-keys'

type VoteInput = {
  ideaId: string
  /** `false` — снять свой голос (`DELETE /api/ideas/{id}/vote`). */
  voted: boolean
}

/** Снимок списков доски для отката, если сервер откажет. */
type VoteContext = {
  snapshot: Array<[readonly unknown[], Idea[] | undefined]>
}

/**
 * Голос за идею (S-60). Голос — переключатель, поэтому одна мутация на оба
 * метода: повторный тап снимает свой голос.
 *
 * Обновляем счётчик оптимистично: между нажатием и ответом кнопка иначе стоит
 * мёртвой, а голосуют подряд по нескольким идеям. Списки после успеха не
 * перезапрашиваем — сервер отвечает 204, новое состояние известно целиком, и
 * тянуть всю доску ради одного числа незачем. Откат — только на ошибке.
 */
export function useVoteIdea(): UseMutationResult<void, Error, VoteInput, VoteContext> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ ideaId, voted }) =>
      apiRequest<void>(`/api/ideas/${ideaId}/vote`, { method: voted ? 'POST' : 'DELETE' }),

    onMutate: async ({ ideaId, voted }) => {
      await queryClient.cancelQueries({ queryKey: ideaKeys.root })
      const snapshot = queryClient.getQueriesData<Idea[]>({ queryKey: ideaKeys.root })

      queryClient.setQueriesData<Idea[]>({ queryKey: ideaKeys.root }, (ideas) =>
        ideas?.map((idea) =>
          idea.id === ideaId
            ? { ...idea, hasVoted: voted, votesCount: idea.votesCount + (voted ? 1 : -1) }
            : idea,
        ),
      )

      return { snapshot }
    },

    onError: (_error, _input, context) => {
      for (const [key, ideas] of context?.snapshot ?? []) {
        queryClient.setQueryData(key, ideas)
      }
    },
  })
}
