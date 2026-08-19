import { RouterProvider } from '@tanstack/react-router'

import { DevPanel } from './dev/DevPanel'
import { AppProviders } from './providers/AppProviders'
import { router } from './router/router'

export function App() {
  return (
    <AppProviders>
      <RouterProvider router={router} />
      {/* import.meta.env.DEV статически вырезает панель из production-сборки. */}
      {import.meta.env.DEV ? <DevPanel /> : null}
    </AppProviders>
  )
}
