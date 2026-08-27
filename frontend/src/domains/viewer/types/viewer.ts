import { type DatingGoal } from '@/domains/onboarding'
import { type UserStatus } from '@/domains/session'

/** Сверено с backend: `Blizka.App/Domain/Enums/SmokingHabit.cs` и `DrinkingHabit.cs` — состав одинаковый. */
export type Habit = 'no' | 'sometimes' | 'regularly'

/** Сверено с backend: `Blizka.App/Domain/Enums/Chronotype.cs`. */
export type Chronotype = 'earlyBird' | 'nightOwl' | 'flexible'

/** Фото профиля — формат общий у `GET /api/users/me` и `/preview`. */
export type ViewerPhoto = {
  id: string
  url: string
  thumbnailUrl: string
  mediumUrl: string
  isMain: boolean
}

/**
 * Сверено с backend: `Blizka.Api/Users/UserMeResponse.cs`.
 *
 * Возраст, название города, фото и интересы сервер готовит сам (фикс T-9.1):
 * считать возраст из `birthDate` и ходить за городом и превью отдельно больше
 * не нужно.
 */
export type Viewer = {
  id: string
  telegramId: number
  name: string
  /** Посчитан на сервере: на границе дня рождения не расходится с лентой. */
  age: number
  gender: 'male' | 'female'
  /** ISO-дата, без времени. */
  birthDate: string
  cityId: string | null
  /** Локализованное название города; пустая строка, пока город не выбран. */
  cityName: string
  bio: string | null
  /** Сантиметры. */
  height: number | null
  smoking: Habit | null
  drinking: Habit | null
  chronotype: Chronotype | null
  prompts: string[]
  datingGoal: DatingGoal | null
  isVerified: boolean
  /** Привязывается один раз при онбординге — профиль (T-9.1) его не меняет. */
  instagramHandle: string | null
  /** Голосовое приветствие. Загрузки пока нет — поле только читаем. */
  voiceIntroUrl: string | null
  photos: ViewerPhoto[]
  interests: Array<{ id: string; name: string }>
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
 * Поля, которые правит `PATCH /api/users/me/profile` (T-9.1).
 * Не переданное поле сервер оставляет как есть — отсюда и `Partial`.
 * Сверено с backend: `Blizka.Api/Users/PatchUserProfileRequest.cs`.
 */
export type ProfilePatch = Partial<{
  name: string
  bio: string | null
  height: number | null
  smoking: Habit | null
  drinking: Habit | null
  chronotype: Chronotype | null
  prompts: string[]
  datingGoal: DatingGoal | null
}>

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
  photos: ViewerPhoto[]
  interests: Array<{ id: string; name: string }>
  prompts: string[]
  isVerified: boolean
}
