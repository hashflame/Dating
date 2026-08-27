import { useNavigate } from '@tanstack/react-router'
import { ArrowLeft } from 'lucide-react'
import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'

import { ROUTES } from '@/shared/config'
import { useBackButton } from '@/shared/telegram'
import { Button } from '@/shared/ui'
import { PhotoGrid } from '@/widgets/photo-grid'

/**
 * Фото профиля (S-40, пункт «Фото»).
 *
 * Та же сетка, что на шаге 4 анкеты: после онбординга фото тоже надо менять,
 * а другого места для этого в приложении не было.
 */
export function ProfilePhotosPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const goBack = useCallback(() => void navigate({ to: ROUTES.profile }), [navigate])
  useBackButton(goBack)

  return (
    <main className="flex flex-col gap-4 px-4 pt-2 pb-safe-5">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" aria-label={t('action.back')} onClick={goBack}>
          <ArrowLeft aria-hidden />
        </Button>
        <h1 className="text-display font-bold">{t('profile.photos')}</h1>
      </div>

      <PhotoGrid />
    </main>
  )
}
