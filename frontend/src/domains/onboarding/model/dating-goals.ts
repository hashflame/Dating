import { type DatingGoal, type ShowGenderPreference } from '../types/onboarding'

/**
 * `as const satisfies` — литеральные ключи i18n сохраняются (типизированный `t()`
 * иначе их не примет), а `satisfies` следит, чтобы значения оставались
 * допустимыми для энумов бэкенда.
 */

/**
 * Кого показывать в ленте. Один список на шаг 2 анкеты и на фильтры ленты:
 * это один и тот же выбор, просто в двух местах.
 */
export const SHOW_GENDER_OPTIONS = [
  { value: 'female', labelKey: 'onboarding.preferences.showFemale' },
  { value: 'male', labelKey: 'onboarding.preferences.showMale' },
  { value: 'all', labelKey: 'onboarding.preferences.showAll' },
] as const satisfies ReadonlyArray<{ value: ShowGenderPreference; labelKey: string }>

/** Цели знакомства из макета S-04 — вместе с эмодзи для крупных карточек выбора. */
export const DATING_GOAL_OPTIONS = [
  {
    value: 'longTermRelationship',
    labelKey: 'onboarding.preferences.goalLongTermRelationship',
    icon: '💍',
  },
  { value: 'familyAndKids', labelKey: 'onboarding.preferences.goalFamilyAndKids', icon: '🏡' },
  { value: 'casual', labelKey: 'onboarding.preferences.goalCasual', icon: '🌿' },
  { value: 'friendship', labelKey: 'onboarding.preferences.goalFriendship', icon: '🤝' },
  { value: 'hobbyCompany', labelKey: 'onboarding.preferences.goalHobbyCompany', icon: '🎲' },
  { value: 'chatting', labelKey: 'onboarding.preferences.goalChatting', icon: '💬' },
] as const satisfies ReadonlyArray<{ value: DatingGoal; labelKey: string; icon: string }>
