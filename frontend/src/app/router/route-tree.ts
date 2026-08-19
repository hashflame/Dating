import { createRootRoute, createRoute } from '@tanstack/react-router'

import { HomePage } from '@/pages/home'
import { SplashPage } from '@/pages/splash'
import { WelcomePage } from '@/pages/welcome'
import { ROUTES } from '@/shared/config'

import { RootLayout } from './RootLayout'

const rootRoute = createRootRoute({ component: RootLayout })

const splashRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.splash,
  component: SplashPage,
})

const welcomeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.welcome,
  component: WelcomePage,
})

const homeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.home,
  component: HomePage,
})

export const routeTree = rootRoute.addChildren([splashRoute, welcomeRoute, homeRoute])
