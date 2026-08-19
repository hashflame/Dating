import { createRootRoute, createRoute } from '@tanstack/react-router'

import { HomePage } from '@/pages/home'
import { ROUTES } from '@/shared/config'

import { RootLayout } from './RootLayout'

const rootRoute = createRootRoute({ component: RootLayout })

const homeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.home,
  component: HomePage,
})

export const routeTree = rootRoute.addChildren([homeRoute])
