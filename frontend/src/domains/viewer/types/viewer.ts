import { type DatingGoal } from '@/domains/onboarding'
import { type UserStatus } from '@/domains/session'

/**
 * Сверено с backend: `Blizka.Api/Users/UserMeResponse.cs`.
 *
 * Города здесь только `cityId` — название берётся отдельным запросом
 * (`useCity`), возраст считается из `birthDate`: сервер их не готовит.
 */
export type Viewer = {
  id: string
  telegramId: number
  name: string
  gender: 'male' | 'female'
  /** ISO-дата, без времени. */
  birthDate: string
  cityId: string | null
  bio: string | null
  /** Сантиметры. */
  height: number | null
  prompts: string[]
  datingGoal: DatingGoal | null
  isVerified: boolean
  /** Баланс «зорок» — внутренней валюты. */
  sparksBalance: number
  status: UserStatus
  locale: string
  /** Заполненность карточки в процентах. */
  profileCompleteness: number
  /** Ближайший недостигнутый порог заполненности. `null` — уже 100%. */
  nextReward: { threshold: number; sparksReward: number; hint: string } | null
}

/**
 * Как анкету видят другие (`GET /api/users/me/preview`).
 * Формат тот же, что у карточки ленты, но без совместимости и расстояния.
 */
export type ViewerPreview = {
  userId: string
  name: string
  age: number
  bio: string | null
  cityName: string
  photos: Array<{
    id: string
    url: string
    thumbnailUrl: string
    mediumUrl: string
    isMain: boolean
  }>
  interests: Array<{ id: string; name: string }>
  prompts: string[]
  isVerified: boolean
}
