import { useNavigate, useParams } from '@tanstack/react-router'
import { ArrowLeft, CalendarCheck, Copy, Lightbulb } from 'lucide-react'
import { useCallback, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useConfirmDate, useDateIdeas, type DateIdea } from '@/domains/matches'
import { ROUTES } from '@/shared/config'
import { copyToClipboard } from '@/shared/lib'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { Button, Card, EmptyState, ErrorState, Skeleton } from '@/shared/ui'

/**
 * Идея свидания (S-39) — ветка хаба мэтча.
 *
 * Варианты собраны по пересечению «Предпочтений на свидания» обоих: это не
 * предложение на конкретный день, а поиск общего формата, поэтому ни даты, ни
 * погоды здесь нет. «Мы договорились» — главный сигнал качества для алгоритма.
 */
export function DateIdeaPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const { matchId } = useParams({ from: ROUTES.matchDateIdea })

  const ideas = useDateIdeas(matchId)
  const confirmDate = useConfirmDate()

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
        <h1 className="text-heading text-display">{t('matches.dateIdea.title')}</h1>
      </div>

      {ideas.isPending && <Skeleton className="h-64 w-full rounded-md" />}
      {ideas.isError && <ErrorState onRetry={() => void ideas.refetch()} />}

      {ideas.data?.ideas.length === 0 && (
        <EmptyState
          icon={Lightbulb}
          title={t('matches.dateIdea.emptyTitle')}
          description={t('matches.dateIdea.emptyDescription')}
        />
      )}

      {ideas.data && ideas.data.ideas.length > 0 && (
        <>
          <p className="text-base text-muted-foreground">{t('matches.dateIdea.description')}</p>

          {ideas.data.ideas.map((idea) => (
            <IdeaCard key={idea.title} idea={idea} />
          ))}

          <Button
            variant="secondary"
            size="lg"
            block
            disabled={confirmDate.isPending || confirmDate.isSuccess}
            onClick={() => {
              haptic.tap()
              confirmDate.mutate(matchId, { onSuccess: () => haptic.success() })
            }}
          >
            <CalendarCheck aria-hidden />
            {confirmDate.isSuccess
              ? t('matches.dateIdea.confirmed')
              : t('matches.dateIdea.confirm')}
          </Button>

          {confirmDate.isError && (
            <p className="text-center text-tiny text-destructive">
              {t('matches.dateIdea.confirmError')}
            </p>
          )}
        </>
      )}
    </main>
  )
}

type IdeaCardProps = {
  idea: DateIdea
}

function IdeaCard({ idea }: IdeaCardProps) {
  const { t } = useTranslation()
  const haptic = useHaptic()
  const [copied, setCopied] = useState(false)

  return (
    <Card padding="tight" className="flex flex-col gap-2">
      <span className="text-base font-semibold">{idea.title}</span>
      <span className="text-tiny text-muted-foreground">{idea.description}</span>

      <span className="flex gap-3 text-tiny text-faint">
        <span>
          {t('matches.dateIdea.cost', { cost: idea.estimatedCost, currency: idea.currency })}
        </span>
        <span>{idea.estimatedDuration}</span>
      </span>

      <Button
        variant="secondary"
        size="sm"
        block
        onClick={() => {
          haptic.success()
          copyToClipboard(idea.inviteText)
          setCopied(true)
        }}
      >
        <Copy aria-hidden />
        {copied ? t('feed.invite.copied') : t('matches.dateIdea.copyInvite')}
      </Button>
    </Card>
  )
}
