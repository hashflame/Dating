import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { sessionKeys } from '@/domains/session'
import { apiRequest } from '@/shared/api'
import { CONSENT_VERSION } from '@/shared/config'

import { type UserConsent } from '../types/consent'

import { onboardingKeys } from './onboarding-keys'

function recordConsent(version: string): Promise<UserConsent> {
  return apiRequest<UserConsent>('/api/users/me/consent', {
    method: 'POST',
    // `ageConfirmed` обязателен для этого типа согласия (закон РБ №99-З):
    // без него бэкенд отвечает 400. Экран показывает один чекбокс сразу
    // про совершеннолетие и про документы, поэтому здесь всегда true.
    body: { type: 'termsAndPrivacyPolicy', version, ageConfirmed: true },
  })
}

/** Фиксирует согласие с Правилами и Политикой. Временную метку ставит сервер. */
export function useRecordConsent(): UseMutationResult<UserConsent, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => recordConsent(CONSENT_VERSION),
    onSuccess: () => {
      // Согласие влияет и на статус пользователя, и на выбор стартового экрана.
      void queryClient.invalidateQueries({ queryKey: sessionKeys.root })
      void queryClient.invalidateQueries({ queryKey: onboardingKeys.consent() })
    },
  })
}
