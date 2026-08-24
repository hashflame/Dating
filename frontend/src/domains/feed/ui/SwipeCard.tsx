import { BadgeCheck, ChevronUp, MapPin } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { cn } from '@/shared/lib'
import { PhotoCarousel } from '@/shared/ui/PhotoCarousel'
import { Tag } from '@/shared/ui/Tag'

import { distanceInKm } from '../lib/describe-place'
import { type FeedCard } from '../types/feed'

type SwipeCardProps = {
  card: FeedCard
  /** Открыть полную анкету. */
  onOpen: () => void
  className?: string
}

/** Сколько общих интересов показываем на карточке — остальное в шторке. */
const VISIBLE_INTERESTS = 3

/**
 * Карточка ленты (S-10): фото на всю высоту, поверх — имя, город и совместимость.
 * Подробности сознательно не дублируем: за ними открывается шторка.
 */
export function SwipeCard({ card, onOpen, className }: SwipeCardProps) {
  const { t } = useTranslation()

  const km = distanceInKm(card)
  const matched = card.interests.filter((interest) => interest.isMatch)
  const shown = (matched.length > 0 ? matched : card.interests).slice(0, VISIBLE_INTERESTS)

  return (
    <article
      className={cn(
        'relative isolate flex flex-col justify-end overflow-hidden rounded-xl bg-card shadow-lg',
        className,
      )}
    >
      <PhotoCarousel
        urls={card.photos.map((photo) => photo.mediumUrl)}
        label={card.name}
        className="absolute inset-0"
      />

      {/* Затемнение под текстом: фото любое, а подписи должны читаться. */}
      <div
        className="pointer-events-none absolute inset-x-0 bottom-0 h-2/3 bg-gradient-to-t from-black/80 via-black/45 to-transparent"
        aria-hidden
      />

      <button
        type="button"
        onClick={onOpen}
        aria-label={t('feed.openProfile', { name: card.name })}
        className="relative flex flex-col items-start gap-2 p-4 text-left outline-none"
      >
        <span className="flex flex-wrap items-center gap-x-2 gap-y-1">
          <span className="text-display leading-none font-bold text-white">
            {card.name}, {card.age}
          </span>
          {card.isVerified && <BadgeCheck className="size-5 text-white" aria-hidden />}
          <span className="rounded-full bg-white/20 px-2 py-0.5 text-tiny font-semibold text-white backdrop-blur">
            {t('feed.compatibility', { score: card.compatibilityScore })}
          </span>
        </span>

        <span className="flex items-center gap-1 text-sm text-white/80">
          <MapPin className="size-3.5" aria-hidden />
          {km === null ? card.cityName : t('feed.cityWithDistance', { city: card.cityName, km })}
        </span>

        <span className="mt-0.5 inline-flex items-center gap-1 rounded-full bg-white/20 px-3 py-1.5 text-sm font-semibold text-white backdrop-blur">
          <ChevronUp className="size-4" aria-hidden />
          {t('feed.showProfile')}
        </span>

        {shown.length > 0 && (
          <span className="flex flex-wrap gap-1.5">
            {shown.map((interest) => (
              <Tag key={interest.id} className="bg-white/20 text-white">
                {interest.name}
              </Tag>
            ))}
          </span>
        )}
      </button>
    </article>
  )
}
