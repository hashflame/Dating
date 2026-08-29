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
  /** Хаб мэтча (S-31) и его ветки — с id мэтча в пути. */
  matchHub: '/matches/$matchId',
  matchDateIdea: '/matches/$matchId/date-idea',
  matchMinigame: '/matches/$matchId/minigame',
  matchStale: '/matches/$matchId/stale',
  ideas: '/ideas',
  profile: '/profile',
  /** Разделы профиля: анкета (интересы и предпочтения теперь в ней), фото, кошелёк зорок (S-46). */
  profileEdit: '/profile/edit',
  profilePhotos: '/profile/photos',
  profileWallet: '/profile/wallet',
  /** Приглашения (S-47). */
  profileInvite: '/profile/invite',
  /** Приватность (S-51) и список заблокированных под ней. */
  profilePrivacy: '/profile/privacy',
  profileBlocked: '/profile/privacy/blocked',
} as const
