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
  onboardingInterests: '/onboarding/interests',
  onboardingDone: '/onboarding/done',
  /** Документы открываются внутри мини-аппа: своего сайта у продукта пока нет. */
  legalTerms: '/legal/terms',
  legalPrivacy: '/legal/privacy',
  /** Лента знакомств — главный экран после анкеты. */
  feed: '/feed',
  /** Разделы нижнего меню. Кроме ленты пока заглушки. */
  likes: '/likes',
  matches: '/matches',
  ideas: '/ideas',
  profile: '/profile',
  /** Разделы профиля: кошелёк зорок (S-46) и интересы (S-43). */
  profileWallet: '/profile/wallet',
  profileInterests: '/profile/interests',
} as const
