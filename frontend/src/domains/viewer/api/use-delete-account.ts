import { useMutation, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

/**
 * Удаляет аккаунт текущего пользователя (T-16.2).
 *
 * Soft delete: бэкенд ставит `Status = Deleted` и `DeletedAt`, данные держит
 * 30 дней. Действие одностороннее — восстановления в API нет, и повторный вход
 * этим же Telegram-id вернёт `410 USER_DELETED`
 * (`AuthenticateTelegramUserCommandHandler`). Идемпотентно: повторный вызов
 * тоже отдаёт 204.
 */
export function useDeleteAccount(): UseMutationResult<void, Error, void> {
  return useMutation({
    mutationFn: () => apiRequest<void>('/api/users/me/account', { method: 'DELETE' }),
  })
}
