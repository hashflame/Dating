import {
  skipToken,
  useMutation,
  useQuery,
  useQueryClient,
  type UseMutationResult,
} from '@tanstack/react-query'

import { sessionKeys } from '@/domains/session'
import { apiRequest } from '@/shared/api'

import { type OnboardingComplete } from '../types/onboarding'

import { onboardingKeys } from './onboarding-keys'

/**
 * Завершает онбординг и начисляет стартовые зорки.
 * Бэкенд ответит 422, если не заполнен шаг, нет согласия или нет ни одного фото,
 * и 409 на повторный вызов — поэтому вызывается только по нажатию кнопки.
 */
export function useCompleteOnboarding(): UseMutationResult<OnboardingComplete, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () =>
      apiRequest<OnboardingComplete>('/api/onboarding/complete', { method: 'POST' }),
    onSuccess: (result) => {
      // Результат читает следующий экран, поэтому кладём его в кэш.
      queryClient.setQueryData(onboardingKeys.completion(), result)
      // Пользователь переходит в Active — статус сессии перечитываем.
      void queryClient.invalidateQueries({ queryKey: sessionKeys.root })
    },
  })
}

/** Результат завершения для экрана «Готово». Только чтение кэша, без запроса. */
export function useCompletionResult(): OnboardingComplete | undefined {
  const { data } = useQuery<OnboardingComplete>({
    queryKey: onboardingKeys.completion(),
    queryFn: skipToken,
    staleTime: Infinity,
  })

  return data
}
