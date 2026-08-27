import { useMutation, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

/**
 * Запрашивает выгрузку своих данных (T-16.2, S-51).
 *
 * Мутация, хотя метод `GET`: сервер отвечает 202 и собирает архив в фоне —
 * ссылка приходит отдельным сообщением в Telegram. Кэшировать тут нечего.
 */
export function useRequestDataExport(): UseMutationResult<void, Error, void> {
  return useMutation({
    mutationFn: () => apiRequest<void>('/api/users/me/data-export'),
  })
}
