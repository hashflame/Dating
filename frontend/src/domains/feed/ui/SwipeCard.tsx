import { BadgeCheck, ChevronUp, MapPin } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { cn, distanceInKm, nameWithAge } from '@/shared/lib'
import { Button } from '@/shared/ui/kit/button'
import { PhotoCarousel } from '@/shared/ui/PhotoCarousel'

import { type FeedCard } from '../types/feed'

type SwipeCardProps = {
  card: FeedCard
  /** Открыть полную анкету. */
  onOpen: () => void
  className?: string
}

/**
 * Карточка ленты (S-10): фото на всю высоту, поверх — имя, возраст и город.
 *
 * Больше на карточке ничего: интересы, совместимость и остальное живут в
 * анкете, и дублировать их здесь значит спорить с ней за внимание. Решение
 * «смотреть дальше или нет» принимается по фото и городу, поэтому кнопка
 * анкеты — во всю ширину, а не подпись, которую легко не заметить.
 */
export function SwipeCard({ card, onOpen, className }: SwipeCardProps) {
  const { t } = useTranslation()

  const km = distanceInKm(card.distanceKm)

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

      <div className="relative flex flex-col gap-3 p-4">
        <div className="flex flex-col gap-1">
          <p className="flex flex-wrap items-center gap-x-2 gap-y-1">
            <span className="text-display leading-none font-bold text-white">
              {nameWithAge(card.name, card.age)}
            </span>
            {card.isVerified && <BadgeCheck className="size-5 text-white" aria-hidden />}
          </p>

          <p className="flex items-center gap-1 text-sm text-white/80">
            <MapPin className="size-3.5" aria-hidden />
            {km === null ? card.cityName : t('feed.cityWithDistance', { city: card.cityName, km })}
          </p>
        </div>

        <Button
          size="lg"
          block
          variant="secondary"
          onClick={onOpen}
          aria-label={t('feed.openProfile', { name: card.name })}
        >
          <ChevronUp aria-hidden />
          {t('feed.showProfile')}
        </Button>
      </div>
    </article>
  )
}
