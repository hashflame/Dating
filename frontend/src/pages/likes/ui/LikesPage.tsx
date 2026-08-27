import { Heart } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useIncomingLikes, useOutgoingLikes, useRevealLikes, type LikeUser } from '@/domains/likes'
import { useUserProfile } from '@/domains/profiles'
import { isApiError } from '@/shared/api'
import { nameWithAge } from '@/shared/lib'
import { useHaptic } from '@/shared/telegram'
import { EmptyState, ErrorState, Skeleton } from '@/shared/ui'
import { SegmentedControl } from '@/shared/ui/SegmentedControl'
import { ProfileSheet } from '@/widgets/profile-sheet'

import { LockedLikes } from './LockedLikes'

type Tab = 'incoming' | 'outgoing'

/**
 * Симпатии (S-21). Две вкладки: кто лайкнул нас и кого лайкнули мы.
 *
 * Названия вкладок не «Вам нравятся»/«Вы нравитесь», как в спеке: по ним
 * невозможно понять, где чьи лайки, и бесплатный список исходящих читался как
 * «вот кто вас лайкнул, и это видно без оплаты». «Вас лайкнули» и «Ваши лайки»
 * однозначны.
 *
 * Входящие до оплаты приходят заблюренными превью без имён — так задумано на
 * бэкенде: `preview` отдаётся вместо `users`, пока раскрытие не оплачено.
 * Платится один раз за весь список, а не за каждого человека.
 */
export function LikesPage() {
  const { t } = useTranslation()
  const [tab, setTab] = useState<Tab>('incoming')
  const [openedId, setOpenedId] = useState<string | undefined>(undefined)

  const incoming = useIncomingLikes()
  const outgoing = useOutgoingLikes()
  // Список отдаёт только имя, возраст и фото — за остальным идём отдельным
  // запросом, когда человека действительно открыли.
  const opened = useUserProfile(openedId)

  /** Число в подписи вкладки появляется, только когда список уже пришёл. */
  const withCount = (label: string, count: number | undefined): string =>
    count === undefined ? label : t('likes.tab.withCount', { label, count })

  return (
    <main className="flex flex-col gap-4 px-4 pt-2 pb-6">
      <SegmentedControl
        value={tab}
        onValueChange={setTab}
        label={t('likes.title')}
        options={[
          {
            value: 'incoming',
            label: withCount(t('likes.tab.incoming'), incoming.data?.count),
          },
          {
            value: 'outgoing',
            label: withCount(t('likes.tab.outgoing'), outgoing.data?.count),
          },
        ]}
      />

      {tab === 'incoming' ? (
        <IncomingTab query={incoming} onOpen={setOpenedId} />
      ) : (
        <OutgoingTab query={outgoing} onOpen={setOpenedId} />
      )}

      <ProfileSheet profile={opened.data ?? null} onClose={() => setOpenedId(undefined)} />
    </main>
  )
}

type IncomingTabProps = {
  query: ReturnType<typeof useIncomingLikes>
  onOpen: (userId: string) => void
}

function IncomingTab({ query, onOpen }: IncomingTabProps) {
  const { t } = useTranslation()
  const haptic = useHaptic()
  const reveal = useRevealLikes()

  if (query.isPending) return <CardsSkeleton />
  if (query.isError) return <ErrorState onRetry={() => void query.refetch()} />

  const likes = query.data
  if (likes.count === 0) {
    return (
      <EmptyState
        icon={Heart}
        title={t('likes.empty.incomingTitle')}
        description={t('likes.empty.incomingDescription')}
      />
    )
  }

  if (likes.revealed) return <UserGrid users={likes.users ?? []} onOpen={onOpen} />

  return (
    <LockedLikes
      previews={likes.preview ?? []}
      count={likes.count}
      unlockCost={likes.unlockCost}
      pending={reveal.isPending}
      onReveal={() => {
        haptic.tap()
        reveal.mutate()
      }}
      error={reveal.isError ? revealFailure(reveal.error, t) : null}
    />
  )
}

type OutgoingTabProps = {
  query: ReturnType<typeof useOutgoingLikes>
  onOpen: (userId: string) => void
}

function OutgoingTab({ query, onOpen }: OutgoingTabProps) {
  const { t } = useTranslation()

  if (query.isPending) return <CardsSkeleton />
  if (query.isError) return <ErrorState onRetry={() => void query.refetch()} />

  if (query.data.count === 0) {
    return (
      <EmptyState
        icon={Heart}
        title={t('likes.empty.outgoingTitle')}
        description={t('likes.empty.outgoingDescription')}
      />
    )
  }

  return <UserGrid users={query.data.users} onOpen={onOpen} />
}

type UserGridProps = {
  users: readonly LikeUser[]
  onOpen: (userId: string) => void
}

/** Имя поверх фото, а не подписью снизу — как на карточке ленты. */
function UserGrid({ users, onOpen }: UserGridProps) {
  const { t } = useTranslation()

  return (
    <ul className="grid grid-cols-2 gap-3">
      {users.map((user) => (
        <li key={user.userId}>
          <button
            type="button"
            onClick={() => onOpen(user.userId)}
            aria-label={t('feed.openProfile', { name: user.name })}
            className="relative isolate block aspect-[4/5] w-full overflow-hidden rounded-lg text-left bg-gradient-photo-1"
          >
            {user.mainPhotoUrl !== null && (
              <img
                src={user.mainPhotoUrl}
                alt=""
                loading="lazy"
                className="absolute inset-0 size-full object-cover"
              />
            )}

            <span
              className="absolute inset-x-0 bottom-0 h-1/2 bg-gradient-to-t from-black/80 to-transparent"
              aria-hidden
            />

            <span className="absolute inset-x-0 bottom-0 block truncate px-3 pb-2.5 text-sm font-semibold text-white">
              {nameWithAge(user.name, user.age)}
            </span>
          </button>
        </li>
      ))}
    </ul>
  )
}

function CardsSkeleton() {
  return (
    <div className="grid grid-cols-2 gap-3">
      {[0, 1, 2, 3].map((index) => (
        <Skeleton key={index} className="aspect-[4/5] w-full rounded-lg" />
      ))}
    </div>
  )
}

/** Почему сервер отказал раскрыть список. */
function revealFailure(
  error: unknown,
  t: (key: 'likes.reveal.noSparks' | 'likes.reveal.error') => string,
): string {
  return isApiError(error) && error.status === 402
    ? t('likes.reveal.noSparks')
    : t('likes.reveal.error')
}
