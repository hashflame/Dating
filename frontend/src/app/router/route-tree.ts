import { createRootRoute, createRoute, lazyRouteComponent } from '@tanstack/react-router'

import { HomePage } from '@/pages/home'
import { SplashPage } from '@/pages/splash'
import { WelcomePage } from '@/pages/welcome'
import { ROUTES } from '@/shared/config'

import { RootLayout } from './RootLayout'

const rootRoute = createRootRoute({ component: RootLayout })

/**
 * Онбординг проходят один раз, поэтому его код грузится отдельным чанком:
 * вернувшийся пользователь его не скачивает.
 */
const onboardingPage = (
  name: 'AboutPage' | 'PreferencesPage' | 'CityPage' | 'PhotosPage' | 'DonePage',
) => lazyRouteComponent(() => import('@/pages/onboarding'), name)

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

const aboutRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.onboardingAbout,
  component: onboardingPage('AboutPage'),
})

const preferencesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.onboardingPreferences,
  component: onboardingPage('PreferencesPage'),
})

const cityRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.onboardingCity,
  component: onboardingPage('CityPage'),
})

const photosRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.onboardingPhotos,
  component: onboardingPage('PhotosPage'),
})

const donePage = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.onboardingDone,
  component: onboardingPage('DonePage'),
})

/** Документы читают редко — держим их отдельным чанком. */
const legalPage = (name: 'TermsPage' | 'PrivacyPage') =>
  lazyRouteComponent(() => import('@/pages/legal'), name)

const legalTermsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.legalTerms,
  component: legalPage('TermsPage'),
})

const legalPrivacyRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.legalPrivacy,
  component: legalPage('PrivacyPage'),
})

const homeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.home,
  component: HomePage,
})

export const routeTree = rootRoute.addChildren([
  splashRoute,
  welcomeRoute,
  aboutRoute,
  preferencesRoute,
  cityRoute,
  photosRoute,
  donePage,
  legalTermsRoute,
  legalPrivacyRoute,
  homeRoute,
])
