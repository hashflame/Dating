import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { SwipeCard } from '@/domains/feed'
import { type ViewerPreview } from '@/domains/viewer'
import { ErrorState, Skeleton } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'
import { ProfileSheet } from '@/widgets/profile-sheet'

type ProfilePreviewSheetProps = {
  open: boolean
  onClose: () => void
  preview: ViewerPreview | undefined
  isPending: boolean
  isError: boolean
  onRetry: () => void
}

/**
 * «Как видят другие» (S-40): сначала карточка, потом анкета.
 *
 * Раньше кнопка открывала сразу полную анкету — то есть показывала второй шаг
 * чужого пути и пропускала первый. А решают по карточке: по главному фото, по
 * тому, читается ли имя на снимке и не пусто ли под ним. Поэтому здесь тот же
 * `SwipeCard`, что и в ленте, вместе с его кнопкой «Показать анкету»: превью
 * должно врать про чужой опыт как можно меньше.
 */
export function ProfilePreviewSheet({
  open,
  onClose,
  preview,
  isPending,
  isError,
  onRetry,
}: ProfilePreviewSheetProps) {
  const { t } = useTranslation()

  // Анкета открывается поверх карточки, а не вместо неё: закрыв её, человек
  // возвращается к карточке — ровно как в ленте.
  const [profileOpen, setProfileOpen] = useState(false)

  const close = (): void => {
    setProfileOpen(false)
    onClose()
  }

  return (
    <>
      <Sheet open={open} onOpenChange={(next) => !next && close()}>
        <SheetContent
          side="bottom"
          closeLabel={t('action.close')}
          className="flex h-[92vh] flex-col gap-3 rounded-t-xl border-0 px-4 pt-5 pb-safe-5"
        >
          <div className="flex flex-col gap-1 pr-10">
            <SheetTitle className="text-lg font-bold">{t('profile.preview')}</SheetTitle>
            <SheetDescription className="text-tiny">{t('profile.previewHint')}</SheetDescription>
          </div>

          {isPending && <Skeleton className="min-h-0 flex-1 rounded-lg" />}
          {isError && <ErrorState onRetry={onRetry} />}

          {preview && (
            <SwipeCard
              card={{ ...preview, distanceKm: null }}
              onOpen={() => setProfileOpen(true)}
              className="min-h-0 flex-1"
            />
          )}
        </SheetContent>
      </Sheet>

      <ProfileSheet
        profile={profileOpen && preview ? preview : null}
        onClose={() => setProfileOpen(false)}
        own
      />
    </>
  )
}
