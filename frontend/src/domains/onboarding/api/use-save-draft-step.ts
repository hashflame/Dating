import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type OnboardingDraft, type OnboardingDraftState } from '../types/onboarding'

import { onboardingKeys } from './onboarding-keys'

type SaveStepInput = {
  /** 1 — о себе, 2 — кого искать, 3 — город. Фото сохраняются отдельным доменом. */
  step: 1 | 2 | 3
  data: OnboardingDraft
}

/** Сохраняет один шаг анкеты, перезаписывая ранее сохранённые данные этого же шага. */
export function useSaveDraftStep(): UseMutationResult<OnboardingDraftState, Error, SaveStepInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ step, data }) =>
      apiRequest<OnboardingDraftState>('/api/onboarding/draft', {
        method: 'PATCH',
        body: { step, data },
      }),
    onSuccess: (draft) => queryClient.setQueryData(onboardingKeys.draft(), draft),
  })
}
