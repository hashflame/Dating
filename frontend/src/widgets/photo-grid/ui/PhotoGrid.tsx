import { Check, Loader2, Plus, Star, X } from 'lucide-react'
import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'

import {
  MAX_PHOTOS,
  useDeletePhoto,
  useImportTelegramPhoto,
  usePhotos,
  useReorderPhotos,
  useUploadPhoto,
} from '@/domains/photos'
import { cn } from '@/shared/lib'
import { getTelegramUser, useHaptic } from '@/shared/telegram'
import { Button, Card, Skeleton } from '@/shared/ui'

/**
 * Сетка фото профиля: загрузка, удаление, выбор главного (S-06).
 *
 * Один виджет на онбординг и на раздел профиля — это буквально один и тот же
 * экран в двух местах, и расходиться они не должны.
 */
export function PhotoGrid() {
  const { t } = useTranslation()
  const haptic = useHaptic()
  const fileInput = useRef<HTMLInputElement>(null)

  const { data: photos, isPending } = usePhotos()
  const upload = useUploadPhoto()
  const importTelegram = useImportTelegramPhoto()
  const deletePhoto = useDeletePhoto()
  const reorder = useReorderPhotos()

  // Загрузка в webview занимает пару секунд, и без явного «готово» непонятно,
  // случилось ли что-нибудь. Держим подтверждение до следующего действия с
  // фото — таймер тут только мигал бы.
  const [uploaded, setUploaded] = useState(false)

  const list = photos ?? []
  const telegramPhotoUrl = getTelegramUser()?.photoUrl
  const canAddMore = list.length < MAX_PHOTOS
  const isUploading = upload.isPending || importTelegram.isPending
  const uploadFailed = upload.isError || importTelegram.isError
  const isBusy = isUploading || reorder.isPending

  // Файл и импорт из Telegram — два независимых мутатора: успех одного не гасит
  // ошибку другого. Поэтому любое новое действие с фото начинает статус с
  // чистого листа, иначе на экране висят «не удалось» и «готово» разом.
  const resetStatus = (): void => {
    setUploaded(false)
    upload.reset()
    importTelegram.reset()
  }

  const handleSetMain = (photoId: string): void => {
    if (list.find((photo) => photo.id === photoId)?.isMain) return

    haptic.select()
    reorder.mutate({
      order: [photoId, ...list.filter((photo) => photo.id !== photoId).map((photo) => photo.id)],
      mainPhotoId: photoId,
    })
  }

  return (
    <>
      {telegramPhotoUrl && canAddMore && (
        <Button
          variant="secondary"
          size="lg"
          block
          disabled={isBusy}
          onClick={() => {
            haptic.tap()
            resetStatus()
            importTelegram.mutate(telegramPhotoUrl, { onSuccess: () => setUploaded(true) })
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
          if (!file) return

          resetStatus()
          upload.mutate(file, { onSuccess: () => setUploaded(true) })
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

            {/* Последнее фото не удалить: сервер отвечает 409, а до онбординга
                оно и так обязательно. Прячем кнопку вместо того, чтобы дать
                нажать и тут же показать ошибку — действие невозможно всегда,
                не иногда. */}
            {list.length > 1 && (
              <button
                type="button"
                onClick={() => {
                  haptic.tap()
                  resetStatus()
                  deletePhoto.mutate(photo.id)
                }}
                aria-label={t('onboarding.photos.remove')}
                className="absolute top-1.5 right-1.5 flex size-6 items-center justify-center rounded-full bg-black/40 text-white"
              >
                <X className="size-3.5" aria-hidden />
              </button>
            )}
          </div>
        ))}

        {isUploading && (
          <div className="flex aspect-[3/4] items-center justify-center rounded-md bg-surface-strong">
            <Loader2 className="size-5 animate-spin text-brand" aria-hidden />
            <span className="sr-only">{t('onboarding.photos.uploading')}</span>
          </div>
        )}

        {!isPending &&
          canAddMore &&
          Array.from(
            { length: Math.max(MAX_PHOTOS - list.length - (isBusy ? 1 : 0), 0) },
            (_, index) => (
              <button
                key={index}
                type="button"
                onClick={() => fileInput.current?.click()}
                disabled={isBusy}
                aria-label={t('onboarding.photos.add')}
                className="flex aspect-[3/4] items-center justify-center rounded-md bg-surface text-muted-foreground transition-colors hover:bg-surface-strong disabled:opacity-50"
              >
                <Plus className="size-5" aria-hidden />
              </button>
            ),
          )}
      </div>

      <Card padding="tight" className="flex flex-col gap-1 text-tiny text-faint">
        <span>★ {t('onboarding.photos.hintMain')}</span>
        <span>💡 {t('onboarding.photos.hintQuality')}</span>
      </Card>

      {/* Один слот на всю загрузку: «идёт», «не вышло» и «готово» —
          взаимоисключающие состояния, показывать их разом нельзя. */}
      <span className="min-h-4 text-center text-tiny" aria-live="polite">
        {isUploading && (
          <span className="text-muted-foreground">{t('onboarding.photos.uploading')}</span>
        )}
        {uploadFailed && !isUploading && (
          <span className="text-destructive">{t('onboarding.photos.uploadError')}</span>
        )}
        {uploaded && !isUploading && !uploadFailed && (
          <span className="inline-flex items-center gap-1 text-moss">
            <Check className="size-3.5" aria-hidden />
            {t('onboarding.photos.uploaded')}
          </span>
        )}
      </span>

      {reorder.isError && (
        <p className="text-center text-tiny text-destructive">{t('onboarding.photos.mainError')}</p>
      )}

      {/* Сообщение сервера уже локализовано и конкретнее любого своего текста
          (T-3.1: «Нельзя удалить последнее фото. Сначала загрузите новое.») —
          показываем как есть. Кнопки не должно быть видно на последнем фото,
          это подстраховка на случай гонки между двумя вкладками. */}
      {deletePhoto.isError && (
        <p className="text-center text-tiny text-destructive">{deletePhoto.error.message}</p>
      )}
    </>
  )
}
