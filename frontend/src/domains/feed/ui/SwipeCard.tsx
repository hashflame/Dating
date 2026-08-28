import { BadgeCheck, Eye, MapPin } from 'lucide-react'
import { type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { cn, distanceInKm, nameWithAge } from '@/shared/lib'
import { Button } from '@/shared/ui/kit/button'
import { PhotoCarousel } from '@/shared/ui/PhotoCarousel'

import { type FeedCard } from '../types/feed'

type SwipeCardProps = {
  card: FeedCard
  /** Открыть полную анкету. */
  onOpen: () => void
  /** Ряд «мимо/нравится»: лежит на самой карточке, на размытой полосе снизу. */
  actions?: ReactNode
  className?: string
}

/**
 * Карточка ленты (S-10): фото на всю высоту, поверх — имя, возраст и город.
 *
 * Больше на карточке ничего: интересы, совместимость и остальное живут в
 * анкете, и дублировать их здесь значит спорить с ней за внимание.
 *
 * Кнопка анкеты по макетам — компактная «стеклянная» пилюля справа от имени,
 * а не полоса во всю ширину: полоса забирала себе низ карточки и читалась
 * как главное действие, хотя главные действия — «мимо» и «нравится».
 *
 * Сами эти действия лежат на карточке, на размытой полосе у её низа: фото
 * тянется до нижнего меню, а кнопки — часть карточки, а не отдельный блок под
 * ней. Так решение и человек, о котором оно принимается, остаются одним целым.
 */
export function SwipeCard({ card, onOpen, actions, className }: SwipeCardProps) {
  const { t } = useTranslation()

  const km = distanceInKm(card.distanceKm)

  return (
    <article
      className={cn(
        'relative isolate flex flex-col justify-end overflow-hidden rounded-lg bg-surface',
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

      <div className="relative flex items-end justify-between gap-3 p-5">
        <div className="flex min-w-0 flex-col gap-1">
          <p className="flex flex-wrap items-center gap-x-2 gap-y-1">
            {/* Имя — антиквой: в макетах это единственный «крупный» текст на
                карточке, и он отличается от интерфейсного гротеска. */}
            <span className="text-display font-bold text-white">
              {nameWithAge(card.name, card.age)}
            </span>
            {card.isVerified && <BadgeCheck className="size-5 shrink-0 text-white" aria-hidden />}
          </p>

          <p className="flex items-center gap-1 text-sm text-white/80">
            <MapPin className="size-3.5 shrink-0" aria-hidden />
            {km === null ? card.cityName : t('feed.cityWithDistance', { city: card.cityName, km })}
          </p>
        </div>

        {/* Стекло, а не заливка: под кнопкой фото, и любой сплошной цвет
            здесь спорит с ним.
            Тонировка — от `brand-foreground` (белый, одинаковый в обеих
            темах), а не от утилиты `glass`: та берёт цвет фона экрана, и в
            светлой теме кнопка становилась белой с белой же подписью. */}
        <Button
          onClick={onOpen}
          variant="ghost"
          aria-label={t('feed.openProfile', { name: card.name })}
          className="shrink-0 gap-1.5 bg-brand-foreground/25 px-4 text-white backdrop-blur-md hover:bg-brand-foreground/35"
        >
          <Eye aria-hidden />
          {t('feed.showProfile')}
        </Button>
      </div>

      {/* Полоса под кнопками: размытие плюс лёгкое затемнение, чтобы кружки
          не тонули в светлом фото. Тонируем чёрным, а не токеном фона: под
          полосой снимок, и в светлой теме фоновый токен выбелил бы её. */}
      {actions && (
        <div className="relative bg-black/25 px-5 pt-3 pb-5 backdrop-blur-xl">{actions}</div>
      )}
    </article>
  )
}
