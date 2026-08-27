import { useNavigate } from '@tanstack/react-router'
import { RotateCcw, SlidersHorizontal, UserPen, UserPlus } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useInviteLink } from '@/domains/referrals'
import { ROUTES } from '@/shared/config'
import { copyToClipboard } from '@/shared/lib'
import { useHaptic } from '@/shared/telegram'
import { Button } from '@/shared/ui'

type FeedExhaustedProps = {
  onExpandFilters: () => void
  /** Вернуть последний свайп. Кнопка ряда действий вместе с декой исчезает. */
  onUndo: () => void
  canUndo: boolean
  /** Почему отмена не удалась — сюда же, чтобы не терялось вместе с декой. */
  undoError?: string
}

/**
 * Анкеты закончились (S-14). Не тупик, а три выхода: расширить фильтры,
 * позвать друзей или заполнить карточку — каждый увеличивает выборку.
 */
export function FeedExhausted({ onExpandFilters, onUndo, canUndo, undoError }: FeedExhaustedProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  // Ссылку тянем сразу: экран «анкеты кончились» и так конечная точка, ждать
  // ещё один запрос после нажатия незачем.
  const invite = useInviteLink()
  const [copied, setCopied] = useState(false)

  const handleInvite = (): void => {
    if (!invite.data) return

    haptic.success()
    copyToClipboard(invite.data.deepLink)
    setCopied(true)
  }

  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-5 px-5 text-center">
      <span
        className="flex size-16 items-center justify-center rounded-full bg-brand-soft"
        aria-hidden
      >
        <SlidersHorizontal className="size-7 text-brand" />
      </span>

      <div className="flex flex-col gap-2">
        <h2 className="text-display font-bold text-balance">{t('feed.exhausted.title')}</h2>
        <p className="text-base text-muted-foreground">{t('feed.exhausted.description')}</p>
      </div>

      <div className="flex w-full flex-col gap-2">
        <Button size="lg" block onClick={onExpandFilters}>
          <SlidersHorizontal aria-hidden />
          {t('feed.exhausted.expandFilters')}
        </Button>

        <Button
          variant="secondary"
          size="lg"
          block
          onClick={handleInvite}
          disabled={invite.isPending || invite.isError}
        >
          <UserPlus aria-hidden />
          {t('feed.exhausted.invite')}
        </Button>

        <Button
          variant="secondary"
          size="lg"
          block
          onClick={() => void navigate({ to: ROUTES.profile })}
        >
          <UserPen aria-hidden />
          {t('feed.exhausted.fillProfile')}
        </Button>

        {/* Ряд кнопок под декой исчез вместе с последней карточкой — а промах
            по последней анкете обиднее всего. Возврат нужен именно здесь. */}
        {canUndo && (
          <Button variant="ghost" size="lg" block onClick={onUndo}>
            <RotateCcw aria-hidden />
            {t('feed.exhausted.undo')}
          </Button>
        )}
      </div>

      <p className="min-h-5 text-tiny" aria-live="polite">
        {copied && <span className="text-moss">{t('feed.invite.copied')}</span>}
        {invite.isError && <span className="text-destructive">{t('feed.invite.error')}</span>}
        {undoError !== undefined && <span className="text-destructive">{undoError}</span>}
      </p>
    </div>
  )
}
