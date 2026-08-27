import { useNavigate } from '@tanstack/react-router'
import { ArrowLeft, Copy, Share2 } from 'lucide-react'
import { useCallback, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useInviteLink, useReferralStats } from '@/domains/referrals'
import { ROUTES } from '@/shared/config'
import { copyToClipboard } from '@/shared/lib'
import { shareToTelegram, useBackButton, useHaptic } from '@/shared/telegram'
import { Button, Card, ErrorState, Skeleton } from '@/shared/ui'

/**
 * Пригласить друга (S-47).
 *
 * Награда отложенная — её начисляют, когда друг дойдёт до конца онбординга,
 * поэтому «приглашено» и «зарегистрировалось» это два разных счётчика, и
 * расходиться они должны честно.
 */
export function InvitePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()

  const invite = useInviteLink()
  const stats = useReferralStats()
  const [copied, setCopied] = useState(false)

  const goBack = useCallback(() => void navigate({ to: ROUTES.profile }), [navigate])
  useBackButton(goBack)

  const handleCopy = (): void => {
    if (!invite.data) return

    haptic.success()
    copyToClipboard(invite.data.deepLink)
    setCopied(true)
  }

  const handleShare = (): void => {
    if (!invite.data) return

    haptic.tap()
    shareToTelegram(invite.data.deepLink, invite.data.shareText)
  }

  return (
    <main className="flex flex-col gap-4 px-4 pt-2 pb-safe-5">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" aria-label={t('action.back')} onClick={goBack}>
          <ArrowLeft aria-hidden />
        </Button>
        <h1 className="text-display font-bold">{t('invite.title')}</h1>
      </div>

      <div className="flex flex-col gap-1">
        <h2 className="text-lg font-bold text-balance">{t('invite.heading')}</h2>
        <p className="text-base text-muted-foreground">{t('invite.description')}</p>
      </div>

      {invite.isPending && <Skeleton className="h-32 w-full rounded-md" />}
      {invite.isError && <ErrorState onRetry={() => void invite.refetch()} />}

      {invite.data && (
        <Card padding="tight" className="flex flex-col gap-3">
          <span className="text-tiny tracking-wide text-faint uppercase">
            {t('invite.linkTitle')}
          </span>

          {/* Ссылку показываем целиком: её копируют глазами, когда буфер обмена
              в webview не сработал. */}
          <span className="rounded-md bg-accent px-3 py-2 text-sm break-all">
            {invite.data.deepLink}
          </span>

          <div className="flex gap-2">
            <Button variant="secondary" size="sm" className="flex-1" onClick={handleCopy}>
              <Copy aria-hidden />
              {t('invite.copy')}
            </Button>

            <Button size="sm" className="flex-1" onClick={handleShare}>
              <Share2 aria-hidden />
              {t('invite.share')}
            </Button>
          </div>

          <span className="min-h-4 text-tiny text-moss" aria-live="polite">
            {copied && t('feed.invite.copied')}
          </span>
        </Card>
      )}

      {stats.data && (
        <div className="grid grid-cols-3 gap-2">
          <StatTile value={stats.data.invited} label={t('invite.stats.invited')} />
          <StatTile value={stats.data.registered} label={t('invite.stats.registered')} />
          <StatTile value={stats.data.sparksEarned} label={t('invite.stats.earned')} />
        </div>
      )}

      <p className="text-tiny text-faint">{t('invite.rewardHint')}</p>
    </main>
  )
}

type StatTileProps = {
  value: number
  label: string
}

function StatTile({ value, label }: StatTileProps) {
  return (
    <Card padding="tight" className="flex flex-col items-center gap-0.5 text-center">
      <span className="text-lg font-bold">{value}</span>
      <span className="text-tiny text-muted-foreground">{label}</span>
    </Card>
  )
}
