import { QueryClientProvider } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'

import { createQueryClient } from './query-client'

type AppProvidersProps = {
  children: ReactNode
}

/** Все глобальные провайдеры приложения. Единственное место, где они объявляются. */
export function AppProviders({ children }: AppProvidersProps) {
  const [queryClient] = useState(createQueryClient)

  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
}
