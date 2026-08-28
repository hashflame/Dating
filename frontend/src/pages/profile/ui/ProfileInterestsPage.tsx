import { useNavigate } from '@tanstack/react-router'
import { ArrowLeft } from 'lucide-react'
import { useCallback, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useSaveInterests } from '@/domains/interests'
import { useViewerPreview } from '@/domains/viewer'
import { ROUTES } from '@/shared/config'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { Button, ErrorState, Skeleton } from '@/shared/ui'
import { InterestPicker, type InterestSelection } from '@/widgets/interest-picker'

const MAX_INTERESTS = 12

/**
 * Интересы в профиле (S-43): тот же выбор, что в онбординге, но сохраняет и
 * возвращает назад, а не завершает анкету.
 *
 * Уже выбранное приходит из `GET /api/users/me/preview` — единственного места,
 * где сервер отдаёт интересы текущего пользователя: в `GET /api/users/me` их нет.
 */
export function ProfileInterestsPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()

  const preview = useViewerPreview(true)
  const save = useSaveInterests()

  // Правки держим отдельно от ответа сервера: пока их нет, показываем
  // сохранённое. Копировать данные в состояние эффектом не нужно — это лишний
  // проход рендера и риск затереть правку пришедшим ответом.
  const [edited, setEdited] = useState<InterestSelection | null>(null)
  const saved: InterestSelection | null = preview.data
    ? { interestIds: preview.data.interests.map((interest) => interest.id), customInterests: [] }
    : null
  const selection = edited ?? saved

  const goBack = useCallback(() => void navigate({ to: ROUTES.profile }), [navigate])
  useBackButton(goBack)

  const handleSave = (): void => {
    if (selection === null) return

    haptic.tap()
    save.mutate(selection, {
      onSuccess: () => {
        haptic.success()
        goBack()
      },
    })
  }

  return (
    <main className="flex flex-1 flex-col gap-3 px-5 pt-2 pb-6">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" aria-label={t('action.back')} onClick={goBack}>
          <ArrowLeft aria-hidden />
        </Button>
        <h1 className="text-display font-bold">{t('profile.interests')}</h1>
      </div>

      {preview.isPending && <Skeleton className="h-40 w-full rounded-md" />}
      {preview.isError && <ErrorState onRetry={() => void preview.refetch()} />}

      {selection && (
        <>
          <InterestPicker value={selection} onChange={setEdited} max={MAX_INTERESTS} />

          {save.isError && (
            <p className="text-center text-tiny text-destructive">{t('onboarding.saveError')}</p>
          )}

          <Button size="lg" block disabled={save.isPending} onClick={handleSave}>
            {t('action.save')}
          </Button>
        </>
      )}
    </main>
  )
}
