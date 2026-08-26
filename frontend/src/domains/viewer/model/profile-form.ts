import { z } from 'zod'

import { type ProfilePatch, type Viewer } from '../types/viewer'

/** Столько ответов на вопросы анкеты принимает бэкенд (`PatchUserProfileCommandValidator`). */
export const MAX_PROMPTS = 3

const HEIGHT_BOUNDS = { min: 100, max: 250 } as const

const habit = z.enum(['no', 'sometimes', 'regularly'])
const chronotype = z.enum(['earlyBird', 'nightOwl', 'flexible'])
const datingGoal = z.enum([
  'longTermRelationship',
  'familyAndKids',
  'casual',
  'friendship',
  'hobbyCompany',
  'chatting',
])

/**
 * Форма правки анкеты (S-40).
 *
 * Все поля — строки: пустая строка значит «не указано», и она же уходит на
 * сервер как `null`. Иначе пришлось бы различать «не трогали» и «стёрли»
 * прямо в контролах, а человек стирает поле именно пустотой.
 *
 * Границы совпадают с `PatchUserProfileCommandValidator` на бэкенде.
 */
export const profileFormSchema = z.object({
  name: z.string().trim().min(2, 'validation.nameTooShort').max(30, 'validation.nameTooLong'),
  bio: z.string().trim().max(500, 'validation.bioTooLong'),
  height: z
    .string()
    .trim()
    .refine(
      (value) =>
        value === '' ||
        (/^\d+$/.test(value) &&
          Number(value) >= HEIGHT_BOUNDS.min &&
          Number(value) <= HEIGHT_BOUNDS.max),
      'validation.heightOutOfRange',
    ),
  smoking: habit.or(z.literal('')),
  drinking: habit.or(z.literal('')),
  chronotype: chronotype.or(z.literal('')),
  datingGoal: datingGoal.or(z.literal('')),
  prompts: z.array(z.string().trim().max(200, 'validation.promptTooLong')).max(MAX_PROMPTS),
})

export type ProfileFormValues = z.infer<typeof profileFormSchema>

/** Заполняет форму сохранённой анкетой: сервер отдаёт `null`, форме нужна пустая строка. */
export function toProfileForm(viewer: Viewer): ProfileFormValues {
  return {
    name: viewer.name,
    bio: viewer.bio ?? '',
    height: viewer.height === null ? '' : String(viewer.height),
    smoking: viewer.smoking ?? '',
    drinking: viewer.drinking ?? '',
    chronotype: viewer.chronotype ?? '',
    datingGoal: viewer.datingGoal ?? '',
    prompts: Array.from({ length: MAX_PROMPTS }, (_, index) => viewer.prompts[index] ?? ''),
  }
}

/**
 * Форма → тело `PATCH /api/users/me/profile`.
 * Пустые ответы на вопросы не отправляем: сервер хранит список без дырок.
 */
export function toProfilePatch(values: ProfileFormValues): ProfilePatch {
  return {
    name: values.name.trim(),
    bio: emptyToNull(values.bio),
    height: values.height.trim() === '' ? null : Number(values.height),
    smoking: values.smoking === '' ? null : values.smoking,
    drinking: values.drinking === '' ? null : values.drinking,
    chronotype: values.chronotype === '' ? null : values.chronotype,
    datingGoal: values.datingGoal === '' ? null : values.datingGoal,
    prompts: values.prompts.map((prompt) => prompt.trim()).filter((prompt) => prompt !== ''),
  }
}

function emptyToNull(value: string): string | null {
  const trimmed = value.trim()

  return trimmed === '' ? null : trimmed
}
