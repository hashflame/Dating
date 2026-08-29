import { useNavigate, useParams } from '@tanstack/react-router'
import { ArrowLeft, Dices, MessagesSquare, Sparkles, Trophy } from 'lucide-react'
import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'

import { ROUTES } from '@/shared/config'
import { useBackButton } from '@/shared/telegram'
import { Button } from '@/shared/ui'
import { ComingSoon } from '@/widgets/coming-soon'

/**
 * Мини-игра — ветка хаба мэтча, пока в разработке.
 *
 * Выключенная строка в хабе не объясняла, что именно готовится, и читалась
 * как поломка. Экран честно показывает план: короткая игра на двоих, которая
 * делает первый шаг за пару.
 */
export function MinigamePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { matchId } = useParams({ from: ROUTES.matchMinigame })

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
        <h1 className="text-display font-bold">{t('matches.minigame.title')}</h1>
      </div>

      <ComingSoon
        title={t('matches.minigame.soon.title')}
        description={t('matches.minigame.soon.description')}
        points={[
          {
            icon: Dices,
            title: t('matches.minigame.soon.short'),
            text: t('matches.minigame.soon.shortText'),
          },
          {
            icon: MessagesSquare,
            title: t('matches.minigame.soon.reason'),
            text: t('matches.minigame.soon.reasonText'),
          },
          {
            icon: Sparkles,
            title: t('matches.minigame.soon.know'),
            text: t('matches.minigame.soon.knowText'),
          },
          {
            icon: Trophy,
            title: t('matches.minigame.soon.result'),
            text: t('matches.minigame.soon.resultText'),
          },
        ]}
      />
    </main>
  )
}
