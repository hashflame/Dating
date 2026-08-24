import { useNavigate } from '@tanstack/react-router'
import { SlidersHorizontal, UserPen, UserPlus } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useInviteFriends } from '@/domains/referrals'
import { ROUTES } from '@/shared/config'
import { useHaptic } from '@/shared/telegram'
import { Button } from '@/shared/ui'

type FeedExhaustedProps = {
  onExpandFilters: () => void
}

/**
 * Анкеты закончились (S-14). Не тупик, а три выхода: расширить фильтры,
 * позвать друзей или заполнить карточку — каждый увеличивает выборку.
 */
export function FeedExhausted({ onExpandFilters }: FeedExhaustedProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const invite = useInviteFriends()
  const [copied, setCopied] = useState(false)

  const handleInvite = (): void => {
    haptic.tap()
    setCopied(false)
    invite.mutate(undefined, {
      onSuccess: ({ link }) => {
        // Буфер обмена есть не во всех webview: результат не ждём, ошибку глотаем —
        // подтверждение показываем в любом случае, ссылка видна в самом тексте.
        void navigator.clipboard?.writeText(link).catch(() => undefined)
        haptic.success()
        setCopied(true)
      },
      onError: () => haptic.error(),
    })
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
          disabled={invite.isPending}
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
      </div>

      <p className="min-h-5 text-tiny" aria-live="polite">
        {copied && <span className="text-moss">{t('feed.invite.copied')}</span>}
        {invite.isError && <span className="text-destructive">{t('feed.invite.error')}</span>}
      </p>
    </div>
  )
}
