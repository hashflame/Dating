import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type UndoResult } from '../types/feed'

import { feedKeys } from './feed-keys'

/**
 * Отменяет последний свайп. Бесплатных отмен ограниченное число, остаток
 * приходит в ответе — по нему решаем, показывать ли кнопку дальше.
 */
export function useUndoSwipe(): UseMutationResult<UndoResult, Error, void> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => apiRequest<UndoResult>('/api/feed/undo', { method: 'POST' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: feedKeys.cards() }),
  })
}
