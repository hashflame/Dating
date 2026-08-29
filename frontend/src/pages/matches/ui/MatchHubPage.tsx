import { useNavigate, useParams } from '@tanstack/react-router'
import { Archive, ArrowLeft, Dices, Lightbulb, MessageCircle, ShieldAlert } from 'lucide-react'
import { useCallback, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useArchiveMatch, useMatchHub } from '@/domains/matches'
import { ROUTES } from '@/shared/config'
import { nameWithAge } from '@/shared/lib'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { Button, Card, ErrorState, ListRow, ProgressBar, Skeleton } from '@/shared/ui'
import { SafetySheet } from '@/widgets/safety-sheet'

import { ContactBranch } from './ContactBranch'

/**
 * Хаб мэтча (S-31) — центральный экран, из которого открываются ветки.
 *
 * Ветки, которых на бэкенде ещё нет, не прячем и не гасим: каждая ведёт на свой
 * экран «в разработке» с планом. Пользователь должен понимать, что такое в
 * приложении будет, иначе хаб выглядит по-разному у разных пар без объяснения.
 */
export function MatchHubPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const { matchId } = useParams({ from: ROUTES.matchHub })

  const hub = useMatchHub(matchId)
  const archive = useArchiveMatch()
  const [safetyOpen, setSafetyOpen] = useState(false)

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
            <span className="size-20 shrink-0 overflow-hidden rounded-lg bg-gradient-photo-1">
              {hub.data.user.mainPhotoUrl !== null && (
                <img src={hub.data.user.mainPhotoUrl} alt="" className="size-full object-cover" />
              )}
            </span>

            <span className="flex min-w-0 flex-col gap-0.5">
              <span className="truncate text-base font-semibold">
                {nameWithAge(hub.data.user.name, hub.data.user.age)}
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
          />

          <section className="flex flex-col gap-1.5">
            <h2 className="text-eyebrow font-bold text-muted-foreground uppercase">
              {t('matches.branchesTitle')}
            </h2>

            <Card padding="none" className="overflow-hidden">
              {/* Все три ветки ведут на экраны «в разработке»: подбора места,
                  мини-игры и подсказок для заглохшего диалога на бэкенде ещё
                  нет. Выключенные строки читались как поломка — вместо этого
                  каждая ветка рассказывает, что именно готовится. */}
              <ListRow
                title={t('matches.branch.dateIdea')}
                subtitle={t('matches.branch.dateIdeaHint')}
                leading={<Lightbulb className="size-5 text-brand" aria-hidden />}
                onClick={() => {
                  haptic.tap()
                  void navigate({ to: ROUTES.matchDateIdea, params: { matchId } })
                }}
              />

              <ListRow
                title={t('matches.branch.minigame')}
                subtitle={t('matches.branch.minigameHint')}
                leading={<Dices className="size-5 text-brand" aria-hidden />}
                onClick={() => {
                  haptic.tap()
                  void navigate({ to: ROUTES.matchMinigame, params: { matchId } })
                }}
              />

              <ListRow
                title={t('matches.branch.stale')}
                subtitle={t('matches.branch.staleHint')}
                leading={<MessageCircle className="size-5 text-brand" aria-hidden />}
                onClick={() => {
                  haptic.tap()
                  void navigate({ to: ROUTES.matchStale, params: { matchId } })
                }}
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

          {/* Безопасность — тоже кнопка, но по смыслу другая: заливка на ступень
              слабее архива, текст и иконка в цвете предупреждения. */}
          <Button
            variant="outline"
            size="lg"
            block
            className="text-destructive hover:bg-destructive/10"
            onClick={() => setSafetyOpen(true)}
          >
            <ShieldAlert aria-hidden />
            {t('feed.safety.open')}
          </Button>

          {archive.isError && (
            <p className="text-center text-tiny text-destructive">{t('matches.archiveError')}</p>
          )}

          <SafetySheet
            userId={safetyOpen ? hub.data.user.userId : null}
            name={hub.data.user.name}
            onClose={() => setSafetyOpen(false)}
            onBlocked={goBack}
          />
        </>
      )}
    </main>
  )
}
