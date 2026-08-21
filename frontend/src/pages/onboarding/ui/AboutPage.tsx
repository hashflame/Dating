import { zodResolver } from '@hookform/resolvers/zod'
import { useNavigate } from '@tanstack/react-router'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import {
  aboutStepSchema,
  useOnboardingDraft,
  useSaveDraftStep,
  type AboutStepValues,
} from '@/domains/onboarding'
import { ROUTES } from '@/shared/config'
import { useFieldError } from '@/shared/i18n'
import { getTelegramUser, useHaptic } from '@/shared/telegram'
import { ErrorState, Field, Input } from '@/shared/ui'
import { SegmentedControl } from '@/shared/ui/SegmentedControl'

import { BirthDatePicker } from './BirthDatePicker'
import { OnboardingStep } from './OnboardingStep'
import { OnboardingStepSkeleton } from './OnboardingStepSkeleton'

const GENDERS = [
  { value: 'male', labelKey: 'onboarding.about.male', icon: '♂' },
  { value: 'female', labelKey: 'onboarding.about.female', icon: '♀' },
] as const

function ageFromIso(iso: string): number | null {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(iso)) return null

  const birth = new Date(iso)
  const now = new Date()
  const monthDiff = now.getMonth() - birth.getMonth()
  const isBefore = monthDiff < 0 || (monthDiff === 0 && now.getDate() < birth.getDate())

  return now.getFullYear() - birth.getFullYear() - (isBefore ? 1 : 0)
}

/** Шаг 1 (S-03): имя, дата рождения, пол. */
export function AboutPage() {
  const { data: draft, isPending, isError, refetch } = useOnboardingDraft()

  if (isPending) return <OnboardingStepSkeleton />
  if (isError) return <ErrorState onRetry={() => void refetch()} />

  // Форма создаётся уже с данными черновика, поэтому не нужен reset в эффекте.
  return (
    <AboutForm
      defaultValues={{
        name: draft.data.name ?? getTelegramUser()?.firstName ?? '',
        birthDate: draft.data.birthDate ?? '',
        gender: draft.data.gender ?? 'male',
      }}
    />
  )
}

type AboutFormProps = {
  defaultValues: AboutStepValues
}

function AboutForm({ defaultValues }: AboutFormProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const fieldError = useFieldError()
  const saveStep = useSaveDraftStep()

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<AboutStepValues>({
    resolver: zodResolver(aboutStepSchema),
    mode: 'onBlur',
    defaultValues,
  })

  const birthDate = watch('birthDate')
  const gender = watch('gender')
  const age = ageFromIso(birthDate)

  const onSubmit = handleSubmit((values) => {
    haptic.tap()
    saveStep.mutate(
      { step: 1, data: values },
      { onSuccess: () => void navigate({ to: ROUTES.onboardingPreferences }) },
    )
  })

  return (
    <OnboardingStep
      step={1}
      title={t('onboarding.about.title')}
      description={t('onboarding.about.description')}
      actionLabel={t('action.next')}
      onAction={() => void onSubmit()}
      actionDisabled={saveStep.isPending}
      onBack={() => void navigate({ to: ROUTES.welcome })}
      error={saveStep.isError ? t('onboarding.saveError') : undefined}
    >
      <Field label={t('onboarding.about.nameLabel')} error={fieldError(errors.name?.message)}>
        <Input
          {...register('name')}
          placeholder={t('onboarding.about.namePlaceholder')}
          aria-invalid={Boolean(errors.name)}
          className="h-11"
        />
      </Field>

      <Field
        label={t('onboarding.about.birthDateLabel')}
        hint={
          age === null
            ? t('onboarding.about.ageHint')
            : `${t('onboarding.about.ageYears', { count: age })} · ${t('onboarding.about.ageHint')}`
        }
        error={fieldError(errors.birthDate?.message)}
      >
        <BirthDatePicker value={birthDate} onChange={(value) => setValue('birthDate', value)} />
      </Field>

      <Field label={t('onboarding.about.genderLabel')}>
        <SegmentedControl
          label={t('onboarding.about.genderLabel')}
          value={gender}
          onValueChange={(next) => {
            haptic.select()
            setValue('gender', next)
          }}
          options={GENDERS.map((option) => ({
            value: option.value,
            label: t(option.labelKey),
            icon: <span aria-hidden>{option.icon}</span>,
          }))}
        />
      </Field>
    </OnboardingStep>
  )
}
