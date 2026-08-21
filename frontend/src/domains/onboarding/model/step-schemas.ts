import { z } from 'zod'

const MIN_AGE = 18
const MAX_AGE = 80

function ageFromIsoDate(iso: string, now = new Date()): number {
  const birth = new Date(iso)
  const monthDiff = now.getMonth() - birth.getMonth()
  const isBeforeBirthday = monthDiff < 0 || (monthDiff === 0 && now.getDate() < birth.getDate())

  return now.getFullYear() - birth.getFullYear() - (isBeforeBirthday ? 1 : 0)
}

/** Шаг 1 (S-03): о себе. Возраст 18+ — то же правило, что и на бэкенде. */
export const aboutStepSchema = z.object({
  name: z.string().trim().min(2, 'validation.nameTooShort').max(32, 'validation.nameTooLong'),
  birthDate: z
    .string()
    .regex(/^\d{4}-\d{2}-\d{2}$/, 'validation.birthDateRequired')
    .refine((value) => ageFromIsoDate(value) >= MIN_AGE, 'validation.ageUnder18'),
  gender: z.enum(['male', 'female'], { message: 'validation.genderRequired' }),
})

export type AboutStepValues = z.infer<typeof aboutStepSchema>

/** Шаг 2 (S-04): кого искать. До двух целей — правило интерфейса, бэкенд лимита не ставит. */
export const preferencesStepSchema = z.object({
  showGender: z.enum(['male', 'female', 'all']),
  ageRange: z
    .object({
      min: z.number().int().min(MIN_AGE).max(MAX_AGE),
      max: z.number().int().min(MIN_AGE).max(MAX_AGE),
    })
    .refine((range) => range.min < range.max, 'validation.ageRangeInvalid'),
  datingGoals: z
    .array(
      z.enum([
        'longTermRelationship',
        'familyAndKids',
        'casual',
        'friendship',
        'hobbyCompany',
        'chatting',
      ]),
    )
    .min(1, 'validation.goalRequired')
    .max(2, 'validation.goalTooMany'),
})

export type PreferencesStepValues = z.infer<typeof preferencesStepSchema>

export const AGE_BOUNDS = { min: MIN_AGE, max: MAX_AGE } as const
