import { QueryClient } from '@tanstack/react-query'

import { isApiError } from '@/shared/api'

/**
 * Настройки кэша по умолчанию для всего приложения.
 * Переопределять их в отдельных хуках можно, но только с комментарием — почему.
 */
export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30 * 1000,
        gcTime: 5 * 60 * 1000,
        retry: (failureCount, error) => {
          // Ошибки клиента повторять бессмысленно.
          if (isApiError(error) && error.status < 500) return false
          return failureCount < 2
        },
        refetchOnWindowFocus: false,
      },
      mutations: {
        retry: false,
      },
    },
  })
}
