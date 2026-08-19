import { RouterProvider } from '@tanstack/react-router'

import { DevThemeToggle } from './dev/DevThemeToggle'
import { AppProviders } from './providers/AppProviders'
import { router } from './router/router'

export function App() {
  return (
    <AppProviders>
      <RouterProvider router={router} />
      {/* import.meta.env.DEV статически вырезает переключатель из production-сборки. */}
      {import.meta.env.DEV ? <DevThemeToggle /> : null}
    </AppProviders>
  )
}
