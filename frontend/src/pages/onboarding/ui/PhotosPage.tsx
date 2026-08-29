import { useNavigate } from '@tanstack/react-router'
import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'

import { usePhotos } from '@/domains/photos'
import { track, useElapsedSeconds } from '@/shared/analytics'
import { ROUTES } from '@/shared/config'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { PhotoGrid } from '@/widgets/photo-grid'

import { OnboardingStep } from './OnboardingStep'

/** Шаг 4 (S-06): фото профиля. Дальше — интересы, они и завершают анкету. */
export function PhotosPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const secondsOnStep = useElapsedSeconds()

  // Сетку целиком держит виджет — здесь список нужен только чтобы не пустить
  // дальше без единого фото.
  const { data: photos } = usePhotos()

  const goBack = useCallback(() => void navigate({ to: ROUTES.onboardingCity }), [navigate])
  useBackButton(goBack)

  const handleNext = (): void => {
    haptic.tap()
    // Фото сохраняются своим доменом по одному: шаг считается пройденным
    // в момент перехода дальше, а не по ответу сервера.
    track({ name: 'onboarding_step_completed', step: 'photos', seconds_on_step: secondsOnStep() })
    void navigate({ to: ROUTES.onboardingInterests })
  }

  return (
    <OnboardingStep
      step={4}
      title={t('onboarding.photos.title')}
      description={t('onboarding.photos.description')}
      actionLabel={t('action.next')}
      onAction={handleNext}
      onBack={goBack}
      actionDisabled={(photos ?? []).length === 0}
    >
      <PhotoGrid />
    </OnboardingStep>
  )
}
