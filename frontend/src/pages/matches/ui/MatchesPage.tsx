import { useNavigate } from '@tanstack/react-router'
import { Archive, ArchiveRestore, Clock, Sparkles } from 'lucide-react'
import { useEffect, type ComponentType } from 'react'
import { useTranslation } from 'react-i18next'

import { useArchiveMatch, useMatches, type MatchUser } from '@/domains/matches'
import { useMarkNotificationsSeen } from '@/domains/notifications'
import { ROUTES } from '@/shared/config'
import { cn, nameWithAge } from '@/shared/lib'
import { useHaptic } from '@/shared/telegram'
import { Button, EmptyState, ErrorState, Skeleton } from '@/shared/ui'
import { MessageLimitsCard } from '@/widgets/message-limits'

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
      {/* Остаток сообщений — первым делом: писать отсюда, и сколько ещё можно
          написать бесплатно, человек должен видеть до того, как откроет мэтч. */}
      <MessageLimitsCard />

      {fresh.length > 0 && (
        <Section title={t('matches.section.new')}>
          {fresh.map((match) => (
            <MatchCard
              key={match.matchId}
              user={match.user}
              subtitle={match.writesFirst ? t('matches.writesFirst') : t('matches.writeFirstHint')}
              subtitleIcon={Sparkles}
              onClick={() => openHub(match.matchId)}
            />
          ))}
        </Section>
      )}

      {waitingForMessage.length > 0 && (
        <Section title={t('matches.section.waiting')}>
          {waitingForMessage.map((match) => (
            <MatchCard
              key={match.matchId}
              user={match.user}
              subtitle={t('matches.contactOpened')}
              subtitleIcon={Clock}
              onClick={() => openHub(match.matchId)}
            />
          ))}
        </Section>
      )}

      {archived.length > 0 && (
        <Section title={t('matches.section.archived')}>
          {archived.map((match) => (
            <MatchCard
              key={match.matchId}
              user={match.user}
              subtitle={t('matches.archived')}
              subtitleIcon={Archive}
              onClick={() => openHub(match.matchId)}
              trailing={
                <Button
                  variant="ghost"
                  size="sm"
                  aria-label={t('matches.restore')}
                  disabled={archive.isPending}
                  onClick={(event) => {
                    event.stopPropagation()
                    toggleArchive(match.matchId, false)
                  }}
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
    <section className="flex flex-col gap-2">
      <h2 className="text-eyebrow font-bold text-muted-foreground uppercase">{title}</h2>
      <div className="flex flex-col gap-2">{children}</div>
    </section>
  )
}

type MatchCardProps = {
  user: MatchUser
  subtitle: string
  subtitleIcon: ComponentType<{ className?: string; 'aria-hidden'?: boolean }>
  onClick: () => void
  trailing?: React.ReactNode
}

function MatchCard({
  user,
  subtitle,
  subtitleIcon: SubtitleIcon,
  onClick,
  trailing,
}: MatchCardProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        'flex w-full items-center gap-3 rounded-lg border border-border bg-card p-3 text-left shadow-sm outline-none',
        'transition-colors duration-150 focus-visible:bg-accent active:bg-accent',
      )}
    >
      <Avatar user={user} />

      <span className="min-w-0 flex-1">
        <span className="block truncate text-base font-bold text-foreground">
          {describeUser(user)}
        </span>
        <span className="mt-0.5 flex items-center gap-1 truncate text-tiny text-faint">
          <SubtitleIcon className="size-3.5 shrink-0" aria-hidden />
          <span className="truncate">{subtitle}</span>
        </span>
      </span>

      {trailing && <span className="shrink-0">{trailing}</span>}
    </button>
  )
}

type AvatarProps = {
  user: MatchUser
}

function Avatar({ user }: AvatarProps) {
  return (
    <span className="size-16 shrink-0 overflow-hidden rounded-full bg-gradient-photo-1">
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
