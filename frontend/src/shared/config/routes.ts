/** Пути роутов. Строки не дублируются по коду — только эти константы. */
export const ROUTES = {
  /** Экран загрузки: проверяет initData и решает, куда вести дальше. */
  splash: '/',
  /** Приветствие и согласие — первый экран нового пользователя. */
  welcome: '/welcome',
  onboardingAbout: '/onboarding/about',
  onboardingPreferences: '/onboarding/preferences',
  onboardingCity: '/onboarding/city',
  onboardingPhotos: '/onboarding/photos',
  onboardingDone: '/onboarding/done',
  /** Заглушка вместо ленты, пока её нет. */
  home: '/home',
} as const
