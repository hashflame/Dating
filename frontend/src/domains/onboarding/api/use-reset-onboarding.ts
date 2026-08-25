import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

/**
 * Удаляет черновик анкеты, возвращает статус пользователя в `new` и чистит
 * его свайпы — то есть возвращает аккаунт к состоянию «регистрация не пройдена»
 * с пустой лентой.
 *
 * Бэкенд называет это debug-утилитой и открыл её в том числе в production:
 * так прогоняют регистрацию и ленту заново, не заводя нового Telegram-пользователя.
 * Зорки, фото, интересы и мэтчи при этом остаются — это не удаление аккаунта.
 */
export function useResetOnboarding(): UseMutationResult<void, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => apiRequest<void>('/api/onboarding/draft', { method: 'DELETE' }),
    // Сброс меняет и статус, и черновик — проще выкинуть весь кэш.
    onSuccess: () => queryClient.invalidateQueries(),
  })
}
