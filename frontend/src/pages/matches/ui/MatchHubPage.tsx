import { useNavigate, useParams } from '@tanstack/react-router'
import { Archive, ArrowLeft, Dices, Lightbulb, MessageCircle, MessagesSquare } from 'lucide-react'
import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'

import { useArchiveMatch, useMatchHub } from '@/domains/matches'
import { ROUTES } from '@/shared/config'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { Button, Card, ErrorState, ListRow, ProgressBar, Skeleton } from '@/shared/ui'

import { ContactBranch } from './ContactBranch'

/**
 * Хаб мэтча (S-31) — центральный экран, из которого открываются ветки.
 *
 * Недоступные ветки не прячем, а показываем выключенными: пользователь должен
 * понимать, что такое в приложении есть, иначе хаб выглядит по-разному у разных
 * пар без объяснения.
 */
export function MatchHubPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const { matchId } = useParams({ from: ROUTES.matchHub })

  const hub = useMatchHub(matchId)
  const archive = useArchiveMatch()

  const goBack = useCallback(() => void navigate({ to: ROUTES.matches }), [navigate])
  useBackButton(goBack)

  return (
    <main className="flex flex-col gap-4 px-4 pt-2 pb-6">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" aria-label={t('action.back')} onClick={goBack}>
          <ArrowLeft aria-hidden />
        </Button>
        <h1 className="truncate text-display font-bold">{hub.data?.user.name ?? ''}</h1>
      </div>

      {hub.isPending && <Skeleton className="h-64 w-full rounded-md" />}
      {hub.isError && <ErrorState onRetry={() => void hub.refetch()} />}

      {hub.data && (
        <>
          <section className="flex items-center gap-4">
            <span className="size-20 shrink-0 overflow-hidden rounded-xl bg-gradient-photo-1">
              {hub.data.user.mainPhotoUrl !== null && (
                <img src={hub.data.user.mainPhotoUrl} alt="" className="size-full object-cover" />
              )}
            </span>

            <span className="flex min-w-0 flex-col gap-0.5">
              <span className="truncate text-base font-semibold">
                {hub.data.user.name}, {hub.data.user.age}
              </span>
              <span className="truncate text-tiny text-muted-foreground">{hub.data.user.city}</span>
            </span>
          </section>

          <Card padding="tight" className="flex flex-col gap-2">
            <span className="flex items-baseline justify-between gap-2">
              <span className="text-base font-semibold">
                {t('feed.compatibility', { score: hub.data.compatibility.score })}
              </span>
            </span>

            <ProgressBar value={hub.data.compatibility.score} />

            <span className="text-tiny text-muted-foreground">
              {hub.data.compatibility.details}
            </span>
          </Card>

          <ContactBranch
            matchId={matchId}
            userId={hub.data.user.userId}
            name={hub.data.user.name}
            status={hub.data.contactStatus}
            cost={hub.data.contactCost}
            telegramUsername={hub.data.user.telegramUsername}
          />

          <section className="flex flex-col gap-1.5">
            <h2 className="text-tiny tracking-wide text-faint uppercase">
              {t('matches.branchesTitle')}
            </h2>

            <Card padding="none" className="overflow-hidden">
              <ListRow
                title={t('matches.branch.question')}
                subtitle={
                  hub.data.features.questionOfDay.available
                    ? t('matches.branch.questionHint')
                    : t('matches.branch.unavailable')
                }
                leading={<MessagesSquare className="size-5 text-brand" aria-hidden />}
                onClick={
                  hub.data.features.questionOfDay.available
                    ? () => {
                        haptic.tap()
                        void navigate({ to: ROUTES.matchQuestion, params: { matchId } })
                      }
                    : undefined
                }
              />

              <ListRow
                title={t('matches.branch.minigame')}
                subtitle={t('matches.branch.unavailable')}
                leading={<Dices className="size-5 text-faint" aria-hidden />}
              />

              <ListRow
                title={t('matches.branch.dateIdea')}
                subtitle={t('matches.branch.unavailable')}
                leading={<Lightbulb className="size-5 text-faint" aria-hidden />}
              />

              <ListRow
                title={t('matches.branch.stale')}
                subtitle={
                  hub.data.features.staleConversation.available
                    ? t('matches.branch.staleHint')
                    : t('matches.branch.unavailable')
                }
                leading={<MessageCircle className="size-5 text-faint" aria-hidden />}
              />
            </Card>
          </section>

          <Button
            variant="secondary"
            size="lg"
            block
            disabled={archive.isPending}
            onClick={() => {
              haptic.tap()
              archive.mutate({ matchId, archived: true }, { onSuccess: goBack })
            }}
          >
            <Archive aria-hidden />
            {t('matches.archiveAction')}
          </Button>

          {archive.isError && (
            <p className="text-center text-tiny text-destructive">{t('matches.archiveError')}</p>
          )}
        </>
      )}
    </main>
  )
}
