import { useNavigate } from '@tanstack/react-router'
import { Archive, ArchiveRestore, Sparkles } from 'lucide-react'
import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'

import { useArchiveMatch, useMatches, type MatchUser } from '@/domains/matches'
import { useMarkNotificationsSeen } from '@/domains/notifications'
import { ROUTES } from '@/shared/config'
import { nameWithAge } from '@/shared/lib'
import { useHaptic } from '@/shared/telegram'
import { Button, Card, EmptyState, ErrorState, ListRow, Skeleton } from '@/shared/ui'

/**
 * Мэтчи (S-30). Три секции приходят готовыми: новые, ждут сообщения, архив.
 *
 * Счётчика «мэтч сгорит через N дней» здесь нет намеренно: по спеке мэтч
 * уходит в архив молча, а возврат бесплатный — угрожать нечем.
 */
export function MatchesPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()

  const matches = useMatches()
  const archive = useArchiveMatch()

  // Бейдж «Мэтчи» считает новые после последнего просмотра (T-10.2) — гасим
  // на успешную загрузку списка, до ранних return'ов: хуки не могут идти
  // после условного выхода.
  const { mutate: markMatchesSeen } = useMarkNotificationsSeen()
  useEffect(() => {
    if (matches.isSuccess) markMatchesSeen({ matches: true })
  }, [matches.isSuccess, markMatchesSeen])

  if (matches.isPending) return <ListSkeleton />
  if (matches.isError) return <ErrorState onRetry={() => void matches.refetch()} />

  const { new: fresh, waitingForMessage, archived } = matches.data
  const empty = fresh.length === 0 && waitingForMessage.length === 0 && archived.length === 0

  if (empty) {
    return (
      <main className="flex flex-1 items-center justify-center px-4">
        <EmptyState
          icon={Sparkles}
          title={t('matches.emptyTitle')}
          description={t('matches.emptyDescription')}
        />
      </main>
    )
  }

  const openHub = (matchId: string): void => {
    haptic.tap()
    void navigate({ to: ROUTES.matchHub, params: { matchId } })
  }

  const toggleArchive = (matchId: string, archivedNow: boolean): void => {
    haptic.tap()
    archive.mutate({ matchId, archived: archivedNow })
  }

  return (
    <main className="flex flex-col gap-5 px-4 pt-2 pb-6">
      {fresh.length > 0 && (
        <Section title={t('matches.section.new')}>
          {fresh.map((match) => (
            <ListRow
              key={match.matchId}
              title={describeUser(match.user)}
              subtitle={
                match.writesFirst
                  ? t('matches.writesFirst')
                  : t('matches.contactCost', { cost: match.contactCost })
              }
              leading={<Avatar user={match.user} />}
              onClick={() => openHub(match.matchId)}
            />
          ))}
        </Section>
      )}

      {waitingForMessage.length > 0 && (
        <Section title={t('matches.section.waiting')}>
          {waitingForMessage.map((match) => (
            <ListRow
              key={match.matchId}
              title={describeUser(match.user)}
              subtitle={t('matches.contactOpened')}
              leading={<Avatar user={match.user} />}
              onClick={() => openHub(match.matchId)}
            />
          ))}
        </Section>
      )}

      {archived.length > 0 && (
        <Section title={t('matches.section.archived')}>
          {archived.map((match) => (
            <ListRow
              key={match.matchId}
              title={describeUser(match.user)}
              subtitle={t('matches.archived')}
              leading={<Avatar user={match.user} />}
              trailing={
                <Button
                  variant="ghost"
                  size="sm"
                  aria-label={t('matches.restore')}
                  disabled={archive.isPending}
                  onClick={() => toggleArchive(match.matchId, false)}
                >
                  <ArchiveRestore aria-hidden />
                </Button>
              }
            />
          ))}
        </Section>
      )}

      {archive.isError && (
        <p className="text-center text-tiny text-destructive">{t('matches.archiveError')}</p>
      )}

      {/* Убрать в архив можно из хаба — здесь только возврат: свайп-действий
          на строках нет, а вторая кнопка в каждой строке спорит с переходом. */}
      {fresh.length > 0 && (
        <p className="flex items-center justify-center gap-1.5 text-tiny text-faint">
          <Archive className="size-3.5" aria-hidden />
          {t('matches.archiveHint')}
        </p>
      )}
    </main>
  )
}

type SectionProps = {
  title: string
  children: React.ReactNode
}

function Section({ title, children }: SectionProps) {
  return (
    <section className="flex flex-col gap-1.5">
      <h2 className="text-tiny tracking-wide text-faint uppercase">{title}</h2>
      <Card padding="none" className="overflow-hidden">
        {children}
      </Card>
    </section>
  )
}

type AvatarProps = {
  user: MatchUser
}

function Avatar({ user }: AvatarProps) {
  return (
    <span className="size-11 shrink-0 overflow-hidden rounded-full bg-gradient-photo-1">
      {user.mainPhotoUrl !== null && (
        <img src={user.mainPhotoUrl} alt="" loading="lazy" className="size-full object-cover" />
      )}
    </span>
  )
}

function describeUser(user: MatchUser): string {
  return nameWithAge(user.name, user.age)
}

function ListSkeleton() {
  return (
    <main className="flex flex-col gap-3 px-4 pt-2">
      {[0, 1, 2].map((index) => (
        <Skeleton key={index} className="h-14 w-full rounded-md" />
      ))}
    </main>
  )
}
