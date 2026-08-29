import { useNavigate } from '@tanstack/react-router'
import { useCallback, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useSaveInterests } from '@/domains/interests'
import { useCompleteOnboarding } from '@/domains/onboarding'
import { track, useElapsedSeconds } from '@/shared/analytics'
import { isApiError } from '@/shared/api'
import { ROUTES } from '@/shared/config'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { InterestPicker, type InterestSelection } from '@/widgets/interest-picker'

import { OnboardingStep } from './OnboardingStep'

/** Продуктовое правило: минимум три интереса, иначе подбор не за что зацепить. */
const MIN_INTERESTS = 3
const MAX_INTERESTS = 12

/** Шаг 5 (S-09): интересы. Завершает онбординг. */
export function InterestsPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()

  const save = useSaveInterests()
  const complete = useCompleteOnboarding()
  const secondsOnStep = useElapsedSeconds()
  const [selection, setSelection] = useState<InterestSelection>({
    interestIds: [],
    customInterests: [],
  })

  const goBack = useCallback(() => void navigate({ to: ROUTES.onboardingPhotos }), [navigate])
  useBackButton(goBack)

  const total = selection.interestIds.length + selection.customInterests.length
  const isBusy = save.isPending || complete.isPending

  /** Сохраняем интересы и сразу завершаем анкету — это последний шаг. */
  const handleFinish = (): void => {
    haptic.tap()
    save.mutate(selection, {
      onSuccess: () =>
        complete.mutate(undefined, {
          onSuccess: (result) => {
            haptic.success()
            track({
              name: 'onboarding_step_completed',
              step: 'interests',
              seconds_on_step: secondsOnStep(),
            })
            track({
              name: 'onboarding_completed',
              profile_completeness: result.profileCompleteness,
            })
            void navigate({ to: ROUTES.onboardingDone })
          },
          onError: (error) => {
            // Анкета уже завершена (409) — вести на «не удалось» неверно.
            if (isApiError(error) && error.code === 'ONBOARDING_ALREADY_COMPLETED') {
              void navigate({ to: ROUTES.feed, replace: true })
              return
            }

            haptic.error()
          },
        }),
    })
  }

  return (
    <OnboardingStep
      step={5}
      title={t('onboarding.interests.title')}
      description={t('onboarding.interests.description', { min: MIN_INTERESTS })}
      actionLabel={t('action.done')}
      onAction={handleFinish}
      onBack={goBack}
      actionDisabled={total < MIN_INTERESTS || isBusy}
      error={save.isError || complete.isError ? t('onboarding.saveError') : undefined}
    >
      <InterestPicker value={selection} onChange={setSelection} max={MAX_INTERESTS} />
    </OnboardingStep>
  )
}
