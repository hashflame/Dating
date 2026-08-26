import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type DatePreferenceCode } from '../types/date-preference'

/**
 * Задаёт предпочтения целиком: сервер заменяет прежний набор присланным.
 *
 * Прочитать сохранённое пока нечем — `GET /api/users/me/date-preferences`
 * отдаёт 405, и в анкете их тоже нет (см. docs/api-gaps.md). Поэтому ответ
 * PATCH — единственный источник актуального набора, кладём его в кэш.
 */
export function useSaveDatePreferences(): UseMutationResult<
  { preferences: unknown[] },
  Error,
  DatePreferenceCode[]
> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (preferences) =>
      apiRequest<{ preferences: unknown[] }>('/api/users/me/date-preferences', {
        method: 'PATCH',
        body: { preferences },
      }),
    // В ответе приходит профиль с новой заполненностью и, возможно, бонус.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['viewer'] }),
  })
}
