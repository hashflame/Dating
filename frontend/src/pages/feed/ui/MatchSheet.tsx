import { Heart } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { type MatchPreview } from '@/domains/feed'
import { cn } from '@/shared/lib'
import { useHaptic } from '@/shared/telegram'
import { Button } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'
import { ComposeSheet } from '@/widgets/compose-sheet'

type MatchSheetProps = {
  /** `null` — мэтча не было или экран закрыт. */
  match: MatchPreview | null
  /** Фото пары: слева наше, справа собеседника. `null` — вместо фото градиент. */
  ownPhotoUrl: string | null
  partnerPhotoUrl: string | null
  onClose: () => void
}

/**
 * Взаимный лайк (S-16).
 *
 * Единственное действие — «Написать»: оно ведёт в шторку первого сообщения
 * (S-33), а оттуда — в личку Telegram с готовым текстом. Контакт за зорки
 * больше не покупается: считается недельный лимит сообщений, цену сверх него
 * показывает сама шторка.
 *
 * Айсбрейкеры отсюда убраны: сервер предлагал ими «Вопрос дня» и «Мини-игру»,
 * а вопроса дня в приложении больше нет, мини-игры не было никогда. Кнопки,
 * которые ведут в несуществующее, хуже, чем их отсутствие.
 */
export function MatchSheet({ match, ownPhotoUrl, partnerPhotoUrl, onClose }: MatchSheetProps) {
  const { t } = useTranslation()
  const haptic = useHaptic()
  /** Открыта ли поверх карточки мэтча шторка первого сообщения. */
  const [composing, setComposing] = useState(false)

  const closeCompose = (): void => {
    setComposing(false)
    onClose()
  }

  return (
    <>
      <Sheet open={match !== null && !composing} onOpenChange={(open) => !open && onClose()}>
        <SheetContent
          side="bottom"
          closeLabel={t('action.close')}
          className="flex max-h-[92vh] flex-col gap-0 overflow-hidden rounded-t-xl p-0"
        >
          {match && (
            <div className="flex min-h-0 flex-col gap-5 overflow-y-auto px-5 pt-5 pb-safe-5">
              <div className="flex flex-col items-center gap-3 pt-2 text-center">
                <span className="rounded-full bg-brand-soft px-3 py-1 text-tiny font-semibold text-brand">
                  {t('feed.match.pill')}
                </span>

                <span className="flex items-center" aria-hidden>
                  <Avatar url={ownPhotoUrl} className="-mr-3" />
                  <Heart className="z-10 size-6 fill-brand text-brand" />
                  <Avatar url={partnerPhotoUrl} className="-ml-3" />
                </span>

                <SheetTitle className="text-display font-bold text-balance">
                  {t('feed.match.title')}
                </SheetTitle>

                <SheetDescription className="text-base">
                  {t('feed.match.subtitle')}
                </SheetDescription>
              </div>

              <div className="flex flex-col gap-2">
                <Button
                  size="lg"
                  block
                  onClick={() => {
                    haptic.tap()
                    setComposing(true)
                  }}
                >
                  {t('feed.match.write')}
                </Button>

                <Button variant="ghost" size="lg" block onClick={onClose}>
                  {t('feed.match.later')}
                </Button>
              </div>
            </div>
          )}
        </SheetContent>
      </Sheet>

      {match && (
        <ComposeSheet
          open={composing}
          onClose={closeCompose}
          userId={match.userId}
          name={match.name}
          kind="message"
          matchId={match.matchId}
          onOpened={closeCompose}
        />
      )}
    </>
  )
}

type AvatarProps = {
  url: string | null
  className: string
}

function Avatar({ url, className }: AvatarProps) {
  return (
    <span className={cn('size-20 overflow-hidden rounded-lg bg-gradient-photo-1', className)}>
      {url !== null && <img src={url} alt="" className="size-full object-cover" />}
    </span>
  )
}
