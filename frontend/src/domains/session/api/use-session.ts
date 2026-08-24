import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest, setAuthToken } from '@/shared/api'

import { USER_STATUSES, type Session, type UserStatus } from '../types/session'

export const sessionKeys = {
  root: ['session'] as const,
  current: () => [...sessionKeys.root, 'current'] as const,
}

/**
 * `status` в ответе входа — обычная строка из `ToString()`, то есть PascalCase
 * («New»), в отличие от остальных энумов API. Приводим к camelCase здесь, на
 * границе, чтобы дальше по коду сравнения были однозначными.
 *
 * Неизвестный статус считаем `active`: лучше пустить человека в ленту, чем
 * запереть в анкете из-за значения, которого мы ещё не знаем.
 */
function toUserStatus(raw: string): UserStatus {
  const camelCase = raw.charAt(0).toLowerCase() + raw.slice(1)

  return USER_STATUSES.find((status) => status === camelCase) ?? 'active'
}

/** Единственный эндпоинт, который принимает initData вместо Bearer. */
async function authenticate(signal: AbortSignal): Promise<Session> {
  const response = await apiRequest<Session>('/api/auth/telegram', {
    method: 'POST',
    auth: 'telegram',
    signal,
  })

  return { ...response, status: toUserStatus(response.status) }
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
