import { createRootRoute, createRoute, lazyRouteComponent } from '@tanstack/react-router'

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
  name: 'AboutPage' | 'PreferencesPage' | 'CityPage' | 'PhotosPage' | 'InterestsPage' | 'DonePage',
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

const interestsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.onboardingInterests,
  component: onboardingPage('InterestsPage'),
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

/** Разделы-заглушки лежат в одном чанке: у них общий экран. */
const soonPage = (name: 'IdeasPage') => lazyRouteComponent(() => import('@/pages/soon'), name)

/** Мэтчи, хаб и вопрос дня — один чанк: это один сценарий. */
const matchesPage = (name: 'MatchesPage' | 'MatchHubPage' | 'QuestionOfDayPage') =>
  lazyRouteComponent(() => import('@/pages/matches'), name)

/** Профиль и его разделы — один чанк: между ними ходят туда-обратно. */
const profilePage = (
  name: 'ProfilePage' | 'WalletPage' | 'ProfileInterestsPage' | 'DatePreferencesPage',
) => lazyRouteComponent(() => import('@/pages/profile'), name)

const likesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.likes,
  component: lazyRouteComponent(() => import('@/pages/likes'), 'LikesPage'),
})

const matchesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.matches,
  component: matchesPage('MatchesPage'),
})

const matchHubRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.matchHub,
  component: matchesPage('MatchHubPage'),
})

const matchQuestionRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.matchQuestion,
  component: matchesPage('QuestionOfDayPage'),
})

const ideasRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.ideas,
  component: soonPage('IdeasPage'),
})

const profileRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.profile,
  component: profilePage('ProfilePage'),
})

const profileWalletRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.profileWallet,
  component: profilePage('WalletPage'),
})

const profileInterestsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.profileInterests,
  component: profilePage('ProfileInterestsPage'),
})

const profileDatePrefsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.profileDatePrefs,
  component: profilePage('DatePreferencesPage'),
})

/** Лента — основной экран, но её код не нужен на входе и в анкете. */
const feedRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.feed,
  component: lazyRouteComponent(() => import('@/pages/feed'), 'FeedPage'),
})

export const routeTree = rootRoute.addChildren([
  splashRoute,
  welcomeRoute,
  aboutRoute,
  preferencesRoute,
  cityRoute,
  photosRoute,
  interestsRoute,
  donePage,
  legalTermsRoute,
  legalPrivacyRoute,
  feedRoute,
  likesRoute,
  matchesRoute,
  matchHubRoute,
  matchQuestionRoute,
  ideasRoute,
  profileRoute,
  profileWalletRoute,
  profileInterestsRoute,
  profileDatePrefsRoute,
])
