import { type DatingGoal, type ShowGenderPreference } from '@/domains/onboarding'

/** Сверено с backend: `Blizka.Api/Feed/FeedDtos.cs`. */
type FeedPhoto = {
  id: string
  url: string
  thumbnailUrl: string
  mediumUrl: string
  isMain: boolean
}

type FeedInterest = {
  id: string
  name: string
  /** Совпал с интересом текущего пользователя — подсвечиваем. */
  isMatch: boolean
}

type CompatibilitySummary = {
  datingGoalMatch: boolean
  sharedInterestsCount: number
  bothVerified: boolean
}

export type FeedCard = {
  userId: string
  name: string
  /** `null` — человек включил «Скрывать возраст» (T-16.1). */
  age: number | null
  bio: string | null
  cityName: string
  /** `null`, если расстояние скрыто или координат нет. */
  distanceKm: number | null
  photos: FeedPhoto[]
  interests: FeedInterest[]
  /** Ответы на вопросы анкеты; порядок задаёт сервер. */
  prompts: string[]
  isVerified: boolean
  compatibilityScore: number
  compatibilitySummary: CompatibilitySummary
  datingGoal: DatingGoal | null
  lastActive: string | null
}

export type Feed = {
  items: FeedCard[]
  /** Анкеты закончились — показываем S-14 вместо дека. */
  exhausted: boolean
  remainingToday: number
}

/**
 * Свайп по карточке ленты. Сверено с backend:
 * `Blizka.App/Domain/Enums/SwipeType.cs`.
 */
export type SwipeAction = 'like' | 'dislike'

type Icebreaker = {
  type: string
  label: string
  effort: string
}

export type MatchPreview = {
  matchId: string
  userId: string
  name: string
  icebreakers: Icebreaker[]
}

export type SwipeResult = {
  action: SwipeAction
  isMatch: boolean
  /** Заполнен только при `isMatch`. */
  match: MatchPreview | null
  sparksBalance: number
}

export type UndoResult = {
  action: SwipeAction
  userId: string
  undosRemaining: number
  sparksBalance: number
}

/** Сверено с backend: `Blizka.Api/Feed/FeedFiltersDtos.cs`. */
export type FeedFilters = {
  showGender: ShowGenderPreference
  ageRange: { min: number; max: number }
  maxDistanceKm: number
  datingGoals: DatingGoal[]
  requireFilledProfile: boolean
  /** `null` — без ограничения по активности. */
  activeWithinDays: number | null
  requirePhoto: boolean
  verifiedOnly: boolean
  nonSmoker: boolean
  nonDrinker: boolean
  noChildren: boolean
}
