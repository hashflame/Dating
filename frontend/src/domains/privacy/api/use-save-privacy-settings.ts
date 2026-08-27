import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { feedKeys } from '@/domains/feed'
import { matchKeys } from '@/domains/matches'
import { apiRequest } from '@/shared/api'

import { type PrivacySettings, type PrivacySettingsPatch } from '../types/privacy'

import { privacyKeys } from './privacy-keys'

/**
 * Сохраняет тумблеры приватности (T-16.1, S-51).
 *
 * Ответ кладём в кэш сразу: сервер возвращает полные настройки, повторный `GET`
 * ничего не добавит. Лента и мэтчи зависят от `hideAge`/`hideDistance`/
 * `blockIncomingMessages` — их инвалидируем.
 */
export function useSavePrivacySettings(): UseMutationResult<
  PrivacySettings,
  Error,
  PrivacySettingsPatch
> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (patch) =>
      apiRequest<PrivacySettings>('/api/privacy/settings', { method: 'PATCH', body: patch }),
    onSuccess: (settings) => {
      queryClient.setQueryData(privacyKeys.settings(), settings)
      void queryClient.invalidateQueries({ queryKey: feedKeys.root })
      void queryClient.invalidateQueries({ queryKey: matchKeys.root })
    },
  })
}
