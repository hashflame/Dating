import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

/** Сверено с backend: `Blizka.Api/Notifications/NotificationDtos.cs`. */
type UnreadNotifications = {
  /** Симпатии, которых пользователь ещё не видел. */
  likes: number
  /** Новые мэтчи. */
  matches: number
}

/**
 * Непрочитанное для бейджей нижнего меню (T-10.2).
 *
 * Раньше бейдж считался по общему числу входящих симпатий и висел всегда —
 * теперь сервер сам говорит, что именно человек ещё не смотрел.
 */
export function useUnreadNotifications(): UseQueryResult<UnreadNotifications, Error> {
  return useQuery({
    queryKey: ['notifications', 'unread'],
    queryFn: ({ signal }) =>
      apiRequest<UnreadNotifications>('/api/notifications/unread', { signal }),
    staleTime: 30 * 1000,
  })
}
