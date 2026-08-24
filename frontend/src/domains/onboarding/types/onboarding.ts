/** Сверено с backend: `Blizka.App/Domain/Enums/Gender.cs`. */
type Gender = 'male' | 'female'

/** Сверено с backend: `Blizka.App/Domain/Enums/ShowGenderPreference.cs`. */
export type ShowGenderPreference = 'male' | 'female' | 'all'

/**
 * Состав — из макета S-04 (шесть целей).
 * Сверено с backend: `Blizka.App/Domain/Enums/DatingGoal.cs` знает все шесть
 * плюс `notSureYet`, которого на экране нет.
 */
export type DatingGoal =
  'longTermRelationship' | 'familyAndKids' | 'casual' | 'friendship' | 'hobbyCompany' | 'chatting'

type AgeRange = { min: number; max: number }

/**
 * Накопленные данные черновика: бэкенд сливает шаги в один объект,
 * поэтому все поля опциональны до своего шага.
 * Сверено с backend: `Blizka.App/UseCases/Onboarding/OnboardingStepData.cs`.
 */
export type OnboardingDraft = {
  name?: string
  /** `YYYY-MM-DD` — на бэкенде `DateOnly`. */
  birthDate?: string
  gender?: Gender
  showGender?: ShowGenderPreference
  ageRange?: AgeRange
  datingGoals?: DatingGoal[]
  cityId?: string
}

/** Ответ `GET|PATCH /api/onboarding/draft`. `step` — номер последнего сохранённого шага, 0 если пусто. */
export type OnboardingDraftState = {
  step: number
  data: OnboardingDraft
}

/** Ответ `POST /api/onboarding/complete`. */
export type OnboardingComplete = {
  sparksAwarded: number
  profileCompleteness: number
  nextReward: { threshold: number; sparksReward: number } | null
}
