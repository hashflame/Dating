import { zodResolver } from '@hookform/resolvers/zod'
import { useNavigate } from '@tanstack/react-router'
import { ArrowLeft } from 'lucide-react'
import { useCallback, type FormEvent } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import { DATING_GOAL_OPTIONS } from '@/domains/onboarding'
import {
  MAX_DATING_GOALS,
  MAX_PROMPTS,
  profileFormSchema,
  toProfileForm,
  toProfilePatch,
  useUpdateProfile,
  useViewer,
  type ProfileFormValues,
  type Viewer,
} from '@/domains/viewer'
import { ROUTES } from '@/shared/config'
import { useFieldError } from '@/shared/i18n'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { Button, ErrorState, Field, Input, Skeleton } from '@/shared/ui'
import { Textarea } from '@/shared/ui/kit/textarea'
import { ToggleGroup } from '@/shared/ui/kit/toggle-group'
import { OptionCard } from '@/shared/ui/OptionCard'
import { SegmentedControl } from '@/shared/ui/SegmentedControl'

/**
 * Варианты привычек ровно те, что принимает бэкенд. Кнопки «—» тут нет
 * намеренно: рядом с «Нет» она читается как второй вариант ответа, хотя
 * означает «не заполнено». Пустое значение показывается тем, что не подсвечен
 * ни один сегмент, а снимается повторным тапом (`allowDeselect`).
 */
const HABIT_OPTIONS = [
  { value: 'no', labelKey: 'profile.edit.habitNo' },
  { value: 'sometimes', labelKey: 'profile.edit.habitSometimes' },
  { value: 'regularly', labelKey: 'profile.edit.habitRegularly' },
] as const

const CHRONOTYPE_OPTIONS = [
  { value: 'earlyBird', labelKey: 'profile.edit.chronotypeEarlyBird' },
  { value: 'nightOwl', labelKey: 'profile.edit.chronotypeNightOwl' },
  { value: 'flexible', labelKey: 'profile.edit.chronotypeFlexible' },
] as const

/**
 * Редактирование анкеты (S-40, пункт «Редактировать карточку»).
 *
 * Своего макета у экрана нет: по карте переходов раздела E это «стандартная
 * форма». Поля ровно те, что принимает `PATCH /api/users/me/profile` — без них
 * заполненность карточки навсегда застревала на 35%, а вместе с ней и зорки за
 * пороги 60/80/100%.
 */
export function ProfileEditPage() {
  const viewer = useViewer()

  if (viewer.isPending) return <EditSkeleton />
  if (viewer.isError) return <ErrorState onRetry={() => void viewer.refetch()} />

  // Форма создаётся уже с анкетой — reset в эффекте не нужен.
  return <ProfileEditForm viewer={viewer.data} />
}

type ProfileEditFormProps = {
  viewer: Viewer
}

function ProfileEditForm({ viewer }: ProfileEditFormProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const fieldError = useFieldError()

  const update = useUpdateProfile()
  const { register, control, handleSubmit, formState } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileFormSchema),
    defaultValues: toProfileForm(viewer),
  })

  const goBack = useCallback(() => void navigate({ to: ROUTES.profile }), [navigate])
  useBackButton(goBack)

  const onSubmit = (values: ProfileFormValues): void => {
    haptic.tap()
    update.mutate(toProfilePatch(values), {
      onSuccess: () => {
        haptic.success()
        goBack()
      },
      onError: () => haptic.error(),
    })
  }

  return (
    <main className="flex flex-col gap-4 px-4 pt-2 pb-safe-5">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" aria-label={t('action.back')} onClick={goBack}>
          <ArrowLeft aria-hidden />
        </Button>
        <h1 className="text-heading text-display">{t('profile.edit.title')}</h1>
      </div>

      <Field label={t('profile.edit.name')} error={fieldError(formState.errors.name?.message)}>
        <Input {...register('name')} />
      </Field>

      <Field
        label={t('profile.edit.bio')}
        hint={t('profile.edit.bioHint')}
        error={fieldError(formState.errors.bio?.message)}
      >
        <Textarea {...register('bio')} rows={4} maxLength={500} />
      </Field>

      <Field
        label={t('profile.edit.height')}
        hint={t('profile.edit.heightHint')}
        error={fieldError(formState.errors.height?.message)}
      >
        <Input {...register('height')} inputMode="numeric" maxLength={3} onInput={digitsOnly} />
      </Field>

      <Controller
        control={control}
        name="smoking"
        render={({ field }) => (
          <Field label={t('profile.edit.smoking')}>
            <SegmentedControl
              value={field.value}
              onValueChange={field.onChange}
              label={t('profile.edit.smoking')}
              allowDeselect
              options={HABIT_OPTIONS.map((option) => ({
                value: option.value,
                label: t(option.labelKey),
              }))}
            />
          </Field>
        )}
      />

      <Controller
        control={control}
        name="drinking"
        render={({ field }) => (
          <Field label={t('profile.edit.drinking')}>
            <SegmentedControl
              value={field.value}
              onValueChange={field.onChange}
              label={t('profile.edit.drinking')}
              allowDeselect
              options={HABIT_OPTIONS.map((option) => ({
                value: option.value,
                label: t(option.labelKey),
              }))}
            />
          </Field>
        )}
      />

      <Controller
        control={control}
        name="chronotype"
        render={({ field }) => (
          <Field label={t('profile.edit.chronotype')}>
            <SegmentedControl
              value={field.value}
              onValueChange={field.onChange}
              label={t('profile.edit.chronotype')}
              allowDeselect
              options={CHRONOTYPE_OPTIONS.map((option) => ({
                value: option.value,
                label: t(option.labelKey),
              }))}
            />
          </Field>
        )}
      />

      <Controller
        control={control}
        name="datingGoals"
        render={({ field }) => (
          <Field label={t('profile.edit.datingGoal')} hint={t('profile.edit.datingGoalHint')}>
            <ToggleGroup
              type="multiple"
              value={field.value}
              onValueChange={(next: string[]) => {
                // Больше двух не даём — то же правило, что и на онбординге (макет S-04).
                field.onChange(next.slice(-MAX_DATING_GOALS))
              }}
              aria-label={t('profile.edit.datingGoal')}
              spacing={2}
              className="grid w-full grid-cols-2"
            >
              {DATING_GOAL_OPTIONS.map((goal) => (
                <OptionCard
                  key={goal.value}
                  value={goal.value}
                  icon={goal.icon}
                  label={t(goal.labelKey)}
                />
              ))}
            </ToggleGroup>
          </Field>
        )}
      />

      <Field label={t('profile.edit.prompts')} hint={t('profile.edit.promptsHint')}>
        <div className="flex flex-col gap-2">
          {Array.from({ length: MAX_PROMPTS }, (_, index) => (
            <Textarea
              key={index}
              {...register(`prompts.${index}`)}
              rows={2}
              maxLength={200}
              placeholder={t('profile.edit.promptPlaceholder', { number: index + 1 })}
            />
          ))}
        </div>
      </Field>

      {update.isError && (
        <p className="text-center text-tiny text-destructive">{t('onboarding.saveError')}</p>
      )}

      <Button
        size="lg"
        block
        disabled={update.isPending}
        onClick={() => void handleSubmit(onSubmit)()}
      >
        {t('action.save')}
      </Button>
    </main>
  )
}

/**
 * Не даём набрать в числовое поле буквы: `inputMode` только подсказывает
 * клавиатуру, а вставить или напечатать можно что угодно. Схема всё равно
 * проверяет, но ловить ошибку после сохранения там, где ввод заведомо
 * бессмысленный, — плохой размен.
 */
function digitsOnly(event: FormEvent<HTMLInputElement>): void {
  const input = event.currentTarget
  const cleaned = input.value.replace(/D/g, '')

  if (cleaned !== input.value) {
    input.value = cleaned
  }
}

function EditSkeleton() {
  return (
    <main className="flex flex-col gap-4 px-4 pt-2">
      <Skeleton className="h-8 w-40" />
      <Skeleton className="h-24 w-full rounded-md" />
      <Skeleton className="h-40 w-full rounded-md" />
    </main>
  )
}
