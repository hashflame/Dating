import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { notificationKeys } from './notification-keys'

type MarkSeenInput = {
  likes?: boolean
  matches?: boolean
}

/**
 * Гасит бейдж(и) нижнего меню (T-10.2) — вызывается при открытии списка
 * симпатий и/или мэтчей. Сервер требует хотя бы один флаг `true`; чтобы вызов
 * оставался однострочным на каждом экране, недостающий флаг здесь же
 * достраивается в `false`, а не перекладывается на вызывающий код.
 */
export function useMarkNotificationsSeen(): UseMutationResult<void, Error, MarkSeenInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ likes = false, matches = false }) =>
      apiRequest<void>('/api/notifications/seen', { method: 'POST', body: { likes, matches } }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: notificationKeys.unread() }),
  })
}
