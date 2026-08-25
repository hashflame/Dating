import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest, setAuthToken } from '@/shared/api'

import { type Session } from '../types/session'

export const sessionKeys = {
  root: ['session'] as const,
  current: () => [...sessionKeys.root, 'current'] as const,
}

/** Единственный эндпоинт, который принимает initData вместо Bearer. */
function authenticate(signal: AbortSignal): Promise<Session> {
  return apiRequest<Session>('/api/auth/telegram', {
    method: 'POST',
    auth: 'telegram',
    signal,
  })
}

/**
 * Сессия текущего пользователя, запрашивается один раз при старте.
 * Токен кладём здесь: это единственное место, где он появляется,
 * и остальные запросы должны увидеть его до своего старта.
 */
export function useSession(): UseQueryResult<Session, Error> {
  return useQuery({
    queryKey: sessionKeys.current(),
    queryFn: async ({ signal }) => {
      const session = await authenticate(signal)
      setAuthToken(session.token)

      return session
    },
    staleTime: Infinity,
    // Невалидный initData повторами не исправить.
    retry: false,
  })
}
