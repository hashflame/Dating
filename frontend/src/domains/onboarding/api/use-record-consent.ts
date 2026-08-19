import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { sessionKeys } from '@/domains/session'
import { apiRequest } from '@/shared/api'
import { CONSENT_VERSION } from '@/shared/config'

import { type UserConsent } from '../types/consent'

function recordConsent(version: string): Promise<UserConsent> {
  return apiRequest<UserConsent>('/api/users/me/consent', {
    method: 'POST',
    body: { type: 'termsAndPrivacyPolicy', version },
  })
}

/** Фиксирует согласие с Правилами и Политикой. Временную метку ставит сервер. */
export function useRecordConsent(): UseMutationResult<UserConsent, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => recordConsent(CONSENT_VERSION),
    // Согласие влияет на то, что бэкенд разрешит дальше, — статус перечитываем.
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: sessionKeys.root }),
  })
}
