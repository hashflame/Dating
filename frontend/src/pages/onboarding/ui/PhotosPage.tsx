import { useNavigate } from '@tanstack/react-router'
import { Plus, Star, X } from 'lucide-react'
import { useCallback, useRef } from 'react'
import { useTranslation } from 'react-i18next'

import { useCompleteOnboarding } from '@/domains/onboarding'
import {
  MAX_PHOTOS,
  useDeletePhoto,
  useImportTelegramPhoto,
  usePhotos,
  useReorderPhotos,
  useUploadPhoto,
} from '@/domains/photos'
import { isApiError } from '@/shared/api'
import { ROUTES } from '@/shared/config'
import { cn } from '@/shared/lib'
import { getTelegramUser, useBackButton, useHaptic } from '@/shared/telegram'
import { Button, Card, Skeleton } from '@/shared/ui'

import { OnboardingStep } from './OnboardingStep'

/** Шаг 4 (S-06): фото профиля. Завершает онбординг. */
export function PhotosPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const fileInput = useRef<HTMLInputElement>(null)

  const { data: photos, isPending } = usePhotos()
  const upload = useUploadPhoto()
  const importTelegram = useImportTelegramPhoto()
  const deletePhoto = useDeletePhoto()
  const reorder = useReorderPhotos()
  const complete = useCompleteOnboarding()

  const goBack = useCallback(() => void navigate({ to: ROUTES.onboardingCity }), [navigate])
  useBackButton(goBack)

  const list = photos ?? []
  const telegramPhotoUrl = getTelegramUser()?.photoUrl
  const canAddMore = list.length < MAX_PHOTOS

  const handleSetMain = (photoId: string): void => {
    if (list.find((photo) => photo.id === photoId)?.isMain) return

    haptic.select()
    reorder.mutate({
      order: [photoId, ...list.filter((photo) => photo.id !== photoId).map((photo) => photo.id)],
      mainPhotoId: photoId,
    })
  }

  const handleFinish = (): void => {
    haptic.tap()
    complete.mutate(undefined, {
      onSuccess: () => {
        haptic.success()
        void navigate({ to: ROUTES.onboardingDone })
      },
      onError: (error) => {
        // Анкета уже завершена (409): показывать «не удалось сохранить» неверно —
        // пользователю просто нечего здесь делать, ведём в ленту.
        if (isApiError(error) && error.code === 'ONBOARDING_ALREADY_COMPLETED') {
          void navigate({ to: ROUTES.home, replace: true })
          return
        }

        haptic.error()
      },
    })
  }

  const isBusy = upload.isPending || importTelegram.isPending || reorder.isPending

  const errorMessage = ((): string | undefined => {
    if (complete.isError) return t('onboarding.saveError')
    if (upload.isError || importTelegram.isError) return t('onboarding.photos.uploadError')

    return undefined
  })()

  return (
    <OnboardingStep
      step={4}
      title={t('onboarding.photos.title')}
      description={t('onboarding.photos.description')}
      actionLabel={t('action.done')}
      onAction={handleFinish}
      onBack={goBack}
      actionDisabled={list.length === 0 || complete.isPending || isBusy}
      error={errorMessage}
    >
      {telegramPhotoUrl && canAddMore && (
        <Button
          variant="secondary"
          size="lg"
          block
          disabled={isBusy}
          onClick={() => {
            haptic.tap()
            importTelegram.mutate(telegramPhotoUrl)
          }}
        >
          {t('onboarding.photos.importTelegram')}
        </Button>
      )}

      <input
        ref={fileInput}
        type="file"
        accept="image/*"
        className="hidden"
        onChange={(event) => {
          const file = event.target.files?.[0]
          event.target.value = ''
          if (file) upload.mutate(file)
        }}
      />

      <div className="grid grid-cols-3 gap-2">
        {isPending && <Skeleton className="col-span-3 aspect-[3/4] w-full" />}

        {list.map((photo) => (
          <div
            key={photo.id}
            className={cn(
              'relative aspect-[3/4] overflow-hidden rounded-lg',
              photo.isMain && 'ring-2 ring-brand',
            )}
          >
            <button
              type="button"
              onClick={() => handleSetMain(photo.id)}
              aria-label={
                photo.isMain ? t('onboarding.photos.main') : t('onboarding.photos.setMain')
              }
              className="block size-full"
            >
              <img
                src={photo.mediumUrl}
                alt=""
                loading="lazy"
                decoding="async"
                className="size-full object-cover"
              />
            </button>

            <span
              className={cn(
                'pointer-events-none absolute top-1.5 left-1.5 flex size-6 items-center justify-center rounded-full text-xs',
                photo.isMain ? 'bg-brand text-brand-foreground' : 'bg-black/40 text-white',
              )}
              aria-hidden
            >
              <Star className="size-3.5" />
            </span>

            <button
              type="button"
              onClick={() => {
                haptic.tap()
                deletePhoto.mutate(photo.id)
              }}
              aria-label={t('onboarding.photos.remove')}
              className="absolute top-1.5 right-1.5 flex size-6 items-center justify-center rounded-full bg-black/40 text-white"
            >
              <X className="size-3.5" aria-hidden />
            </button>
          </div>
        ))}

        {!isPending &&
          canAddMore &&
          Array.from({ length: MAX_PHOTOS - list.length }, (_, index) => (
            <button
              key={index}
              type="button"
              onClick={() => fileInput.current?.click()}
              disabled={isBusy}
              aria-label={t('onboarding.photos.add')}
              className="flex aspect-[3/4] items-center justify-center rounded-lg border border-border text-faint disabled:opacity-50"
            >
              <Plus className="size-5" aria-hidden />
            </button>
          ))}
      </div>

      <Card padding="tight" className="flex flex-col gap-1 text-tiny text-faint">
        <span>★ {t('onboarding.photos.hintMain')}</span>
        <span>💡 {t('onboarding.photos.hintQuality')}</span>
      </Card>
    </OnboardingStep>
  )
}
