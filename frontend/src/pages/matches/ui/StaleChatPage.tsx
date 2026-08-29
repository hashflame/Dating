import { useNavigate, useParams } from '@tanstack/react-router'
import { ArrowLeft, BellOff, Clock, MessageCircle, Wand2 } from 'lucide-react'
import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'

import { ROUTES } from '@/shared/config'
import { useBackButton } from '@/shared/telegram'
import { Button } from '@/shared/ui'
import { ComingSoon } from '@/widgets/coming-soon'

/**
 * «Диалог заглох» — ветка хаба мэтча, пока в разработке.
 *
 * Подсказки для возврата разговора собирает бэкенд, их ещё нет. Показываем,
 * что готовится, вместо выключенной строки без объяснения.
 */
export function StaleChatPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { matchId } = useParams({ from: ROUTES.matchStale })

  const goBack = useCallback(
    () => void navigate({ to: ROUTES.matchHub, params: { matchId } }),
    [navigate, matchId],
  )
  useBackButton(goBack)

  return (
    <main className="flex flex-col gap-4 px-4 pt-2 pb-safe-5">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" aria-label={t('action.back')} onClick={goBack}>
          <ArrowLeft aria-hidden />
        </Button>
        <h1 className="text-display font-bold">{t('matches.stale.title')}</h1>
      </div>

      <ComingSoon
        title={t('matches.stale.soon.title')}
        description={t('matches.stale.soon.description')}
        points={[
          {
            icon: MessageCircle,
            title: t('matches.stale.soon.reason'),
            text: t('matches.stale.soon.reasonText'),
          },
          {
            icon: Clock,
            title: t('matches.stale.soon.timing'),
            text: t('matches.stale.soon.timingText'),
          },
          {
            icon: Wand2,
            title: t('matches.stale.soon.options'),
            text: t('matches.stale.soon.optionsText'),
          },
          {
            icon: BellOff,
            title: t('matches.stale.soon.quiet'),
            text: t('matches.stale.soon.quietText'),
          },
        ]}
      />
    </main>
  )
}
