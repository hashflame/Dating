import { BadgeCheck, Eye, MapPin } from 'lucide-react'
import { type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { cn, distanceInKm, nameWithAge } from '@/shared/lib'
import { Button } from '@/shared/ui/kit/button'
import { PhotoCarousel } from '@/shared/ui/PhotoCarousel'

import { type FeedCard } from '../types/feed'

/**
 * Что карточке нужно от анкеты. Не `FeedCard` целиком: тем же видом
 * показывается своя анкета в «как видят другие», а совместимости, целей и
 * активности у превью нет — и на карточке они всё равно не рисуются.
 */
export type SwipeCardProfile = Pick<FeedCard, 'name' | 'age' | 'cityName' | 'isVerified'> & {
  photos: ReadonlyArray<{ id: string; mediumUrl: string }>
  /** `null` или отсутствует — расстояние скрыто, у своей анкеты его нет вовсе. */
  distanceKm?: number | null
}

type SwipeCardProps = {
  card: SwipeCardProfile
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

      {/* Низ карточки одним блоком: размытие лежит под подписями и кнопками
          сразу, поэтому текст остаётся чётким, а размывается только снимок. */}
      <div className="relative">
        {actions && <ProgressiveBlur />}

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
              {km === null
                ? card.cityName
                : t('feed.cityWithDistance', { city: card.cityName, km })}
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

        {actions && <div className="relative px-5 pb-5">{actions}</div>}
      </div>
    </article>
  )
}

/**
 * Размытие под кнопками, нарастающее сверху вниз.
 *
 * Одна полоса с `backdrop-blur` давала видимую границу поперёк фото — резкий
 * шов ровно там, где начинается размытие. Здесь несколько слоёв с разной
 * силой размытия, и каждый следующий проявляется маской ниже предыдущего:
 * переход от чёткого снимка к размытому низу получается плавным.
 *
 * Слой заходит выше подписей (`-top-16`), иначе нарастать размытию негде.
 */
function ProgressiveBlur() {
  return (
    <div className="pointer-events-none absolute inset-x-0 -top-16 bottom-0" aria-hidden>
      {BLUR_LAYERS.map(({ blur, from }) => (
        <div
          key={blur}
          className="absolute inset-0"
          style={{
            backdropFilter: `blur(${blur}px)`,
            WebkitBackdropFilter: `blur(${blur}px)`,
            maskImage: `linear-gradient(to bottom, transparent ${from}%, black ${from + 25}%)`,
            WebkitMaskImage: `linear-gradient(to bottom, transparent ${from}%, black ${from + 25}%)`,
          }}
        />
      ))}

      {/* Лёгкое затемнение к низу: на светлом снимке одного размытия мало,
          чтобы белые подписи и кружки читались. Тонируем чёрным, а не токеном
          фона: под полосой фото, и в светлой теме токен выбелил бы её. */}
      <div className="absolute inset-0 bg-gradient-to-b from-transparent to-black/35" />
    </div>
  )
}

/** Сила размытия удваивается от слоя к слою — линейный рост глазом не читается. */
const BLUR_LAYERS = [
  { blur: 2, from: 0 },
  { blur: 4, from: 25 },
  { blur: 8, from: 50 },
  { blur: 16, from: 75 },
] as const
