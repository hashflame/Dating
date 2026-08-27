import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { viewerKeys } from '@/domains/viewer'
import { apiRequest } from '@/shared/api'

import { ideaKeys } from './idea-keys'

type CreateIdeaInput = {
  text: string
  anonymous: boolean
}

type CreateIdeaResult = {
  /**
   * Сколько зорок начислили за эту идею. `0` — начисление в этом месяце уже
   * было: по T-19.1 бонус даётся раз в месяц, и человеку важно понимать, что
   * идея принята, но зорки за неё в этот раз не пришли.
   */
  sparksAwarded: number
}

/** Предложить идею (S-60). За первую идею в месяце начисляют зорки. */
export function useCreateIdea(): UseMutationResult<CreateIdeaResult, Error, CreateIdeaInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ text, anonymous }) =>
      apiRequest<CreateIdeaResult>('/api/ideas', { method: 'POST', body: { text, anonymous } }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ideaKeys.root })
      // Начисление меняет баланс — он виден в шапке и в кошельке.
      void queryClient.invalidateQueries({ queryKey: viewerKeys.root })
    },
  })
}
