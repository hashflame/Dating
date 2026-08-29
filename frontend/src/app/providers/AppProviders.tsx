import { QueryClientProvider } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'

import { AnalyticsProvider } from './AnalyticsProvider'
import { createQueryClient } from './query-client'

type AppProvidersProps = {
  children: ReactNode
}

/** Все глобальные провайдеры приложения. Единственное место, где они объявляются. */
export function AppProviders({ children }: AppProvidersProps) {
  const [queryClient] = useState(createQueryClient)

  return (
    <QueryClientProvider client={queryClient}>
      {/* Внутри QueryClientProvider: аналитике нужны сессия и профиль. */}
      <AnalyticsProvider>{children}</AnalyticsProvider>
    </QueryClientProvider>
  )
}
