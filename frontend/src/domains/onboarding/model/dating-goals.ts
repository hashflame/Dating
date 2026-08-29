import { type ComponentType } from 'react'

import {
  ChatBubbleIcon,
  DiceIcon,
  FriendsIcon,
  HomeHeartIcon,
  RingsIcon,
  SproutIcon,
  type IconProps,
} from '@/shared/ui/icons'

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

/**
 * Цели знакомства из макета S-04 — вместе с иконкой для крупных карточек выбора.
 *
 * Иконки свои (`shared/ui/icons`), а не эмодзи: системные эмодзи рисуются
 * шрифтом платформы — на iOS это цветные картинки Apple, на Android другие,
 * и одна и та же карточка выглядит в разных клиентах по-разному. Свои контуры
 * наследуют цвет карточки, в том числе на залитой выбранной.
 */
export const DATING_GOAL_OPTIONS = [
  {
    value: 'longTermRelationship',
    labelKey: 'onboarding.preferences.goalLongTermRelationship',
    Icon: RingsIcon,
  },
  {
    value: 'familyAndKids',
    labelKey: 'onboarding.preferences.goalFamilyAndKids',
    Icon: HomeHeartIcon,
  },
  { value: 'casual', labelKey: 'onboarding.preferences.goalCasual', Icon: SproutIcon },
  { value: 'friendship', labelKey: 'onboarding.preferences.goalFriendship', Icon: FriendsIcon },
  { value: 'hobbyCompany', labelKey: 'onboarding.preferences.goalHobbyCompany', Icon: DiceIcon },
  { value: 'chatting', labelKey: 'onboarding.preferences.goalChatting', Icon: ChatBubbleIcon },
] as const satisfies ReadonlyArray<{
  value: DatingGoal
  labelKey: string
  Icon: ComponentType<IconProps>
}>
