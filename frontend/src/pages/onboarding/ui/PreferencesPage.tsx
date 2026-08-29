import { useNavigate } from '@tanstack/react-router'
import { useCallback, useState } from 'react'
import { useTranslation } from 'react-i18next'

import {
  AGE_BOUNDS,
  DATING_GOAL_OPTIONS,
  SHOW_GENDER_OPTIONS,
  preferencesStepSchema,
  useOnboardingDraft,
  useSaveDraftStep,
  type DatingGoal,
  type PreferencesStepValues,
  type ShowGenderPreference,
} from '@/domains/onboarding'
import { ROUTES } from '@/shared/config'
import { useFieldError } from '@/shared/i18n'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { ErrorState, Field } from '@/shared/ui'
import { ToggleGroup } from '@/shared/ui/kit/toggle-group'
import { OptionCard } from '@/shared/ui/OptionCard'
import { RangeField } from '@/shared/ui/RangeField'
import { SegmentedControl } from '@/shared/ui/SegmentedControl'

import { OnboardingStep } from './OnboardingStep'
import { OnboardingStepSkeleton } from './OnboardingStepSkeleton'

/**
 * Правило из макета S-04. Живёт только здесь: серверной проверки нет и мы её
 * не просим — ограничение продуктовое, а не про целостность данных.
 * Бэкенд проверяет лишь, что список непустой.
 */
const MAX_GOALS = 2
const DEFAULT_AGE_RANGE = { min: 22, max: 32 }

/** Шаг 2 (S-04): кого показывать, возраст, цели знакомства. */
export function PreferencesPage() {
  const { data: draft, isPending, isError, refetch } = useOnboardingDraft()

  if (isPending) return <OnboardingStepSkeleton />
  if (isError) return <ErrorState onRetry={() => void refetch()} />

  return (
    <PreferencesForm
      defaultValues={{
        showGender: draft.data.showGender ?? 'all',
        ageRange: draft.data.ageRange ?? DEFAULT_AGE_RANGE,
        datingGoals: draft.data.datingGoals ?? [],
      }}
    />
  )
}

type PreferencesFormProps = {
  defaultValues: PreferencesStepValues
}

function PreferencesForm({ defaultValues }: PreferencesFormProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const fieldError = useFieldError()
  const saveStep = useSaveDraftStep()

  const [showGender, setShowGender] = useState<ShowGenderPreference>(defaultValues.showGender)
  const [ageRange, setAgeRange] = useState<[number, number]>([
    defaultValues.ageRange.min,
    defaultValues.ageRange.max,
  ])
  const [goals, setGoals] = useState<DatingGoal[]>(defaultValues.datingGoals)
  const [validationError, setValidationError] = useState<string | undefined>(undefined)

  const goBack = useCallback(() => void navigate({ to: ROUTES.onboardingAbout }), [navigate])
  useBackButton(goBack)

  const handleGoalsChange = (next: string[]): void => {
    haptic.select()
    // Больше двух не даём: правило интерфейса из макета.
    setGoals(next.slice(-MAX_GOALS) as DatingGoal[])
  }

  const handleNext = (): void => {
    // Интерфейс не даёт собрать невалидное состояние, но схема — зеркало контракта:
    // если правила разойдутся, ошибка вылезет здесь, а не в 400 от бэкенда.
    const parsed = preferencesStepSchema.safeParse({
      showGender,
      ageRange: { min: ageRange[0], max: ageRange[1] },
      datingGoals: goals,
    })

    if (!parsed.success) {
      setValidationError(parsed.error.issues[0]?.message)
      haptic.error()
      return
    }

    haptic.tap()
    setValidationError(undefined)
    saveStep.mutate(
      { step: 2, data: parsed.data },
      { onSuccess: () => void navigate({ to: ROUTES.onboardingCity }) },
    )
  }

  const errorMessage = ((): string | undefined => {
    if (validationError) return fieldError(validationError)
    if (saveStep.isError) return t('onboarding.saveError')

    return undefined
  })()

  return (
    <OnboardingStep
      step={2}
      title={t('onboarding.preferences.title')}
      actionLabel={t('action.next')}
      onAction={handleNext}
      onBack={goBack}
      actionDisabled={goals.length === 0 || saveStep.isPending}
      error={errorMessage}
    >
      <SegmentedControl
        label={t('onboarding.preferences.title')}
        value={showGender}
        onValueChange={(next) => {
          haptic.select()
          setShowGender(next)
        }}
        options={SHOW_GENDER_OPTIONS.map((option) => ({
          value: option.value,
          label: t(option.labelKey),
        }))}
      />

      <Field label={t('onboarding.preferences.ageLabel')}>
        <RangeField
          value={ageRange}
          onChange={setAgeRange}
          min={AGE_BOUNDS.min}
          max={AGE_BOUNDS.max}
          suffix={t('onboarding.preferences.ageSuffix')}
          fromLabel={t('onboarding.preferences.ageFromLabel')}
          toLabel={t('onboarding.preferences.ageToLabel')}
        />
      </Field>

      <Field
        label={t('onboarding.preferences.goalsLabel')}
        aside={t('onboarding.preferences.goalsHint')}
      >
        <ToggleGroup
          type="multiple"
          value={goals}
          onValueChange={handleGoalsChange}
          aria-label={t('onboarding.preferences.goalsLabel')}
          // spacing > 0 — карточки отдельные: у shadcn при spacing=0 группа
          // склеивается в полосу, и в сетке первая с последней выглядят иначе.
          spacing={2}
          className="grid w-full grid-cols-2"
        >
          {DATING_GOAL_OPTIONS.map((goal) => (
            <OptionCard
              key={goal.value}
              value={goal.value}
              icon={<goal.Icon />}
              label={t(goal.labelKey)}
            />
          ))}
        </ToggleGroup>
      </Field>
    </OnboardingStep>
  )
}
