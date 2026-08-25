import { useCallback, useState } from 'react'
import { useTranslation } from 'react-i18next'

import {
  SwipeCard,
  useFeed,
  useFeedFilters,
  useSwipe,
  useUndoSwipe,
  type FeedCard,
  type MatchPreview,
  type SwipeAction,
} from '@/domains/feed'
import { usePhotos } from '@/domains/photos'
import { isApiError } from '@/shared/api'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { ErrorState, Skeleton } from '@/shared/ui'
import { ProfileSheet } from '@/widgets/profile-sheet'

import { FeedExhausted } from './FeedExhausted'
import { FeedFiltersSheet } from './FeedFiltersSheet'
import { MatchSheet } from './MatchSheet'
import { SwipeActions } from './SwipeActions'
import { SwipeDeck } from './SwipeDeck'

/**
 * Лента знакомств (S-10) — главный экран после анкеты.
 *
 * Дек показывает одну верхнюю карточку: остальные приходят тем же запросом
 * и ждут своей очереди в кэше, поэтому следующая анкета появляется без задержки.
 */
export function FeedPage() {
  const { t } = useTranslation()
  const haptic = useHaptic()

  const feed = useFeed()
  const swipe = useSwipe()
  const undo = useUndoSwipe()

  const [filtersOpen, setFiltersOpen] = useState(false)
  // Счётчик открытий — ключ шторки фильтров. Смена ключа пересоздаёт её форму,
  // иначе черновик инициализируется один раз и во второй раз показывает
  // прошлые значения вместо сохранённых. Анимация закрытия при этом сохраняется:
  // ключ меняется только на открытии.
  const [filtersSession, setFiltersSession] = useState(0)
  const [opened, setOpened] = useState<FeedCard | null>(null)
  const [match, setMatch] = useState<MatchPreview | null>(null)
  // Фото собеседника берём из карточки: в ответе о мэтче его нет.
  const [matchPhotoUrl, setMatchPhotoUrl] = useState<string | null>(null)
  // Остаток бесплатных отмен. `null` — неизвестен: узнать его до первой отмены
  // нечем (эндпоинта нет), поэтому считаем доступной и полагаемся на ответ
  // сервера. Иначе после перезагрузки кнопка пропадала бы, хотя отмена работает.
  const [undosLeft, setUndosLeft] = useState<number | null>(null)
  const [undoError, setUndoError] = useState<string | undefined>(undefined)

  // Фильтры подгружаем заранее: шторка открывается уже с данными, без скелетона.
  const filters = useFeedFilters()
  const ownPhotos = usePhotos()
  const ownPhotoUrl = ownPhotos.data?.find((photo) => photo.isMain)?.mediumUrl ?? null

  const card = feed.data?.items.at(0)

  // Нативная кнопка «Назад» закрывает верхнюю шторку. Лента — корневой экран,
  // поэтому без открытых шторок кнопки нет: уходить с неё некуда.
  const anySheetOpen = opened !== null || match !== null || filtersOpen
  const openFilters = (): void => {
    setFiltersSession((value) => value + 1)
    setFiltersOpen(true)
  }

  const closeTopSheet = useCallback(() => {
    setMatch(null)
    setOpened(null)
    setFiltersOpen(false)
  }, [])
  useBackButton(anySheetOpen ? closeTopSheet : undefined)

  const handleSwipe = (action: SwipeAction): void => {
    if (!card) return

    haptic.tap()
    swipe.mutate(
      { userId: card.userId, action },
      {
        onSuccess: (result) => {
          setUndoError(undefined)
          if (!result.match) return

          haptic.success()
          setMatchPhotoUrl(card.photos.find((photo) => photo.isMain)?.mediumUrl ?? null)
          setMatch(result.match)
        },
        onError: () => haptic.error(),
      },
    )
  }

  const handleUndo = (): void => {
    haptic.tap()
    setUndoError(undefined)
    undo.mutate(undefined, {
      onSuccess: (result) => setUndosLeft(result.undosRemaining),
      onError: (error) => {
        haptic.error()
        if (!isApiError(error)) return

        // Бесплатные отмены исчерпаны — кнопка больше не нужна. «Нечего
        // отменять» состояние временное: после следующего свайпа она снова нужна.
        if (error.code === 'UNDO_LIMIT_EXCEEDED') {
          setUndosLeft(0)
          setUndoError(t('feed.undoSpent'))
          return
        }

        setUndoError(
          error.code === 'NOTHING_TO_UNDO' ? t('feed.nothingToUndo') : t('feed.swipeError'),
        )
      },
    })
  }

  const swipeError = ((): string | undefined => {
    if (!swipe.isError) return undefined
    if (isApiError(swipe.error) && swipe.error.status === 402) return t('feed.noSparks')

    return t('feed.swipeError')
  })()

  return (
    <main className="flex flex-1 flex-col gap-3 overflow-hidden px-4 pt-1 pb-4">
      {feed.isPending && <Skeleton className="flex-1 rounded-xl" />}

      {feed.isError && (
        <div className="flex flex-1 items-center justify-center">
          <ErrorState onRetry={() => void feed.refetch()} />
        </div>
      )}

      {feed.isSuccess && !card && <FeedExhausted onExpandFilters={openFilters} />}

      {card && (
        <>
          <SwipeDeck
            cardId={card.userId}
            onSwipe={handleSwipe}
            disabled={swipe.isPending || undo.isPending}
          >
            <SwipeCard card={card} onOpen={() => setOpened(card)} className="flex-1" />
          </SwipeDeck>

          {/* Строка держит место всегда: иначе появление ошибки дёргало бы деку. */}
          <p className="min-h-4 text-center text-tiny text-destructive" aria-live="polite">
            {swipeError ?? undoError}
          </p>

          <SwipeActions
            onDislike={() => handleSwipe('dislike')}
            onLike={() => handleSwipe('like')}
            onUndo={handleUndo}
            onOpenFilters={openFilters}
            canUndo={undosLeft === null || undosLeft > 0}
            disabled={swipe.isPending || undo.isPending}
          />
        </>
      )}

      <ProfileSheet profile={opened} onClose={() => setOpened(null)} />
      <MatchSheet
        match={match}
        ownPhotoUrl={ownPhotoUrl}
        partnerPhotoUrl={matchPhotoUrl}
        onClose={() => setMatch(null)}
      />

      {filters.data && (
        <FeedFiltersSheet
          key={filtersSession}
          open={filtersOpen}
          onClose={() => setFiltersOpen(false)}
          filters={filters.data}
        />
      )}
    </main>
  )
}
