import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { setSavedDatePreferences } from '../lib/saved-preferences'
import { type DatePreferenceCode } from '../types/date-preference'

type SaveInput = {
  /** Чей выбор запоминаем на устройстве — см. `saved-preferences.ts`. */
  userId: string
  preferences: DatePreferenceCode[]
}

/**
 * Задаёт предпочтения целиком: сервер заменяет прежний набор присланным.
 *
 * Прочитать сохранённое пока нечем — `GET /api/users/me/date-preferences`
 * отдаёт 405, и в анкете их тоже нет (см. docs/api-gaps.md). Поэтому
 * сохранённый набор дублируем на устройство: иначе форма правки анкеты
 * открывается с пустым выбором и стирает его при следующем сохранении.
 */
export function useSaveDatePreferences(): UseMutationResult<
  { preferences: unknown[] },
  Error,
  SaveInput
> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ preferences }) =>
      apiRequest<{ preferences: unknown[] }>('/api/users/me/date-preferences', {
        method: 'PATCH',
        body: { preferences },
      }),
    // В ответе приходит профиль с новой заполненностью и, возможно, бонус.
    onSuccess: (_result, { userId, preferences }) => {
      setSavedDatePreferences(userId, preferences)

      return queryClient.invalidateQueries({ queryKey: ['viewer'] })
    },
  })
}
