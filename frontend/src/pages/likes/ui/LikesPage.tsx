import { useNavigate } from '@tanstack/react-router'
import { Heart, MessageCircleHeart, SlidersHorizontal } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useSwipe, type SwipeAction } from '@/domains/feed'
import { useIncomingLikes, useOutgoingLikes, useRevealLikes, type LikeUser } from '@/domains/likes'
import { useMarkNotificationsSeen } from '@/domains/notifications'
import { useUserProfile } from '@/domains/profiles'
import { isApiError } from '@/shared/api'
import { ROUTES } from '@/shared/config'
import { cn, nameWithAge } from '@/shared/lib'
import { useHaptic } from '@/shared/telegram'
import { EmptyState, ErrorState, Skeleton } from '@/shared/ui'
import { SegmentedControl } from '@/shared/ui/SegmentedControl'
import { useSetAppBarAction } from '@/widgets/app-bar'
import { ProfileSheet } from '@/widgets/profile-sheet'
import { SafetySheet } from '@/widgets/safety-sheet'

import { LikesSettingsSheet } from './LikesSettingsSheet'
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
 *
 * Суперсообщений здесь нет: за них заплатил отправитель, поэтому они приходят
 * раскрытыми и живут во вкладке «Мэтчи».
 */
export function LikesPage() {
  const { t } = useTranslation()
  const haptic = useHaptic()
  const navigate = useNavigate()
  const [tab, setTab] = useState<Tab>('incoming')
  const [openedId, setOpenedId] = useState<string | undefined>(undefined)
  const [safetyUser, setSafetyUser] = useState<{ id: string; name: string } | null>(null)
  // Смэтченные больше не пропадают из списка молча (тикет ClickUp) — по умолчанию
  // показываем всех, а спрятать их можно переключателем в настройках экрана.
  const [hideMatched, setHideMatched] = useState(false)
  const [settingsOpen, setSettingsOpen] = useState(false)

  // Стабильная ссылка: колбэк уезжает в шапку через контекст, и новый объект
  // на каждый рендер гонял бы её эффект по кругу.
  const openSettings = useCallback(() => setSettingsOpen(true), [])
  // Настройки списка живут в шапке, как фильтры в ленте: над сеткой карточек
  // не остаётся служебной строки.
  useSetAppBarAction(SlidersHorizontal, t('likes.action.settings'), openSettings)

  const incoming = useIncomingLikes()
  const outgoing = useOutgoingLikes()

  // Бейдж «Симпатии» считает входящие после последнего просмотра (T-10.2) —
  // гасим его, как только список действительно загрузился, а не по факту
  // одного открытия экрана: если запрос упал, отмечать нечего. Деструктурируем
  // `mutate` сразу: это стабильная ссылка react-query, а весь объект мутации
  // пересоздаётся на каждый рендер и увёл бы эффект в бесконечный перезапуск.
  const { mutate: markLikesSeen } = useMarkNotificationsSeen()
  useEffect(() => {
    if (incoming.isSuccess) markLikesSeen({ likes: true })
  }, [incoming.isSuccess, markLikesSeen])
  // Список отдаёт только имя, возраст и фото — за остальным идём отдельным
  // запросом, когда человека действительно открыли.
  const opened = useUserProfile(openedId)

  // Ответ на входящую симпатию. Тот же свайп, что и в ленте: эти люди в ленту
  // больше не попадут, и без кнопок здесь взаимный лайк было некому поставить.
  const swipe = useSwipe()
  const [decisionError, setDecisionError] = useState<string | undefined>(undefined)

  const decide = async (action: SwipeAction): Promise<void> => {
    if (openedId === undefined) return

    haptic.tap()
    setDecisionError(undefined)

    try {
      const result = await swipe.mutateAsync({ userId: openedId, action })
      setOpenedId(undefined)

      // Лайк в ответ на лайк — это мэтч. Ведём сразу в его хаб: следующий
      // осмысленный шаг — написать, а не вернуться к списку.
      if (result.match) {
        haptic.success()
        void navigate({ to: ROUTES.matchHub, params: { matchId: result.match.matchId } })
      }
    } catch {
      haptic.error()
      setDecisionError(t('feed.swipeError'))
    }
  }

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
        <IncomingTab query={incoming} onOpen={setOpenedId} hideMatched={hideMatched} />
      ) : (
        <OutgoingTab query={outgoing} onOpen={setOpenedId} hideMatched={hideMatched} />
      )}

      <LikesSettingsSheet
        open={settingsOpen}
        onClose={() => setSettingsOpen(false)}
        hideMatched={hideMatched}
        onHideMatchedChange={setHideMatched}
      />

      <ProfileSheet
        profile={opened.data ?? null}
        onClose={() => {
          setOpenedId(undefined)
          setDecisionError(undefined)
        }}
        // Только во входящих: в «Ваших лайках» человек уже свайпнут, и второй
        // раз то же решение принимать не из чего.
        decision={
          tab === 'incoming'
            ? {
                onLike: () => void decide('like'),
                onDislike: () => void decide('dislike'),
                pending: swipe.isPending,
                error: decisionError,
              }
            : undefined
        }
        onSafety={() => {
          if (!opened.data) return

          setSafetyUser({ id: opened.data.userId, name: opened.data.name })
          setOpenedId(undefined)
        }}
      />

      <SafetySheet
        userId={safetyUser?.id ?? null}
        name={safetyUser?.name ?? ''}
        onClose={() => setSafetyUser(null)}
      />
    </main>
  )
}

type IncomingTabProps = {
  query: ReturnType<typeof useIncomingLikes>
  onOpen: (userId: string) => void
  hideMatched: boolean
}

function IncomingTab({ query, onOpen, hideMatched }: IncomingTabProps) {
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

  if (likes.revealed) {
    return <UserGrid users={likes.users ?? []} onOpen={onOpen} hideMatched={hideMatched} />
  }

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
  hideMatched: boolean
}

function OutgoingTab({ query, onOpen, hideMatched }: OutgoingTabProps) {
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

  return <UserGrid users={query.data.users} onOpen={onOpen} hideMatched={hideMatched} />
}

type UserGridProps = {
  users: readonly LikeUser[]
  onOpen: (userId: string) => void
  hideMatched: boolean
}

/**
 * Имя поверх фото, а не подписью снизу — как на карточке ленты.
 *
 * Смэтченные и те, кому ушло суперсообщение, помечены цветной полосой во всю
 * ширину у нижнего края, а не пилюлей в углу: пилюля тонула в пёстром снимке —
 * фирменный красный на фото читался как часть кадра (тикет ClickUp). Полоса
 * непрозрачная и во всю ширину, поэтому видна при любом кадре; мэтч вдобавок
 * обведён фирменной рамкой — его видно, даже не читая подпись.
 *
 * Смэтченные по тапу ведут в хаб мэтча, а не в карточку профиля: раз мэтч уже
 * есть, следующий осмысленный шаг — написать, а не посмотреть анкету ещё раз.
 */
function UserGrid({ users, onOpen, hideMatched }: UserGridProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const visible = hideMatched ? users.filter((user) => !user.isMatched) : users

  return (
    <ul className="grid grid-cols-2 gap-3">
      {visible.map((user) => (
        <li key={user.userId}>
          <button
            type="button"
            onClick={() =>
              user.isMatched && user.matchId !== null
                ? void navigate({ to: ROUTES.matchHub, params: { matchId: user.matchId } })
                : onOpen(user.userId)
            }
            aria-label={
              user.isMatched
                ? t('likes.openMatch', { name: user.name })
                : t('feed.openProfile', { name: user.name })
            }
            className={cn(
              'relative isolate block aspect-[4/5] w-full overflow-hidden rounded-lg text-left bg-gradient-photo-1',
              user.isMatched && 'ring-2 ring-brand',
            )}
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

            {/* Имя и полоса стоят стопкой поверх нижнего края фото, а не
                отрезают от него кусок: иначе у помеченных карточек снимок
                ниже соседних, и подписи в ряду разъезжаются по высоте. */}
            <span className="absolute inset-x-0 bottom-0 flex flex-col">
              <span className="block truncate px-3 pb-2.5 text-sm font-semibold text-white">
                {nameWithAge(user.name, user.age)}
              </span>

              <CardMarker user={user} />
            </span>
          </button>
        </li>
      ))}
    </ul>
  )
}

/**
 * Полоса под фото: мэтч или отправленное суперсообщение.
 *
 * Мэтч сильнее — если он есть, суперсообщение уже неважно: человек ответил.
 * Суперсообщение помечено тише (своя заливка, фирменный только в тексте):
 * это напоминание, что писать второй раз не нужно, а не событие.
 */
function CardMarker({ user }: { user: LikeUser }) {
  const { t } = useTranslation()

  if (user.isMatched) {
    return (
      <span className="flex items-center gap-1.5 bg-brand px-2.5 py-1.5 text-tiny font-bold text-brand-foreground">
        <Heart className="size-3.5 shrink-0 fill-current" aria-hidden />
        <span className="truncate">{t('likes.matchBadge')}</span>
      </span>
    )
  }

  if (user.superMessageSent === true) {
    return (
      <span className="flex items-center gap-1.5 bg-surface px-2.5 py-1.5 text-tiny font-semibold text-brand">
        <MessageCircleHeart className="size-3.5 shrink-0" aria-hidden />
        <span className="truncate">{t('likes.superSentBadge')}</span>
      </span>
    )
  }

  return null
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
