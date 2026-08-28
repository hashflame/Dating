import { Eye } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { cn } from '@/shared/lib'
import { Button } from '@/shared/ui'

/** Раскладка мозаики по числу превью. Индекс — сколько плиток показываем. */
const GRID_BY_TILES = [
  'grid-cols-1',
  'grid-cols-1',
  'grid-cols-2',
  'grid-cols-2 grid-rows-2',
  'grid-cols-2 grid-rows-2',
] as const

type LockedLikesProps = {
  previews: ReadonlyArray<{ blurredPhotoUrl: string }>
  count: number
  unlockCost: number
  pending: boolean
  onReveal: () => void
  error: string | null
}

/**
 * Закрытый список входящих симпатий (S-21): мозаика из размытых фото, поверх
 * неё — сколько человек и кнопка разблокировки.
 *
 * Размытие приходит с сервера, но здесь оно ещё усилено и растянуто: превью
 * маленькие, и без этого сквозь них читались бы лица. Мозаика одним блоком,
 * а не сеткой карточек, потому что до оплаты это не список людей, а «сколько
 * их там» — сетка карточек обещала бы, что по ним можно тапнуть.
 */
export function LockedLikes({
  previews,
  count,
  unlockCost,
  pending,
  onReveal,
  error,
}: LockedLikesProps) {
  const { t } = useTranslation()

  // Раскладка по числу превью: одно фото — во весь блок, два — в столбцы,
  // больше — сетка 2×2. Дублировать одно фото в четыре плитки нельзя: получается
  // видимый шов и ощущение поломки, а не мозаики.
  const tiles = previews.slice(0, 4)
  const grid = GRID_BY_TILES[Math.min(tiles.length, 4)]

  return (
    <div className="flex flex-col gap-3">
      <div className="relative isolate overflow-hidden rounded-lg">
        <div className={cn('grid aspect-[4/5]', grid)} aria-hidden>
          {tiles.map((tile, index) => (
            <div key={index} className="overflow-hidden bg-gradient-photo-1">
              {tile && (
                <img
                  src={tile.blurredPhotoUrl}
                  alt=""
                  className="size-full scale-125 object-cover blur-lg"
                />
              )}
            </div>
          ))}
        </div>

        {/* Затемнение поверх мозаики: под ним читается белый текст на любых
            фото, включая почти белые — на них одной полупрозрачной заливки мало,
            поэтому сверху ещё градиент. */}
        <div
          className="absolute inset-0 bg-black/55 bg-gradient-to-t from-black/45 to-black/10"
          aria-hidden
        />

        <div className="absolute inset-0 flex flex-col items-center justify-center gap-4 p-6 text-center">
          <span className="flex size-12 items-center justify-center rounded-full bg-white/15 backdrop-blur">
            <Eye className="size-6 text-white" aria-hidden />
          </span>

          <p className="text-lg font-bold text-balance text-white">
            {t('likes.reveal.rated', { count })}
          </p>

          <Button size="lg" disabled={pending} onClick={onReveal}>
            {t('likes.reveal.action', { cost: unlockCost })}
          </Button>

          {error !== null && <p className="text-tiny text-white/90">{error}</p>}
        </div>
      </div>

      <p className="rounded-md bg-muted px-4 py-3 text-tiny text-muted-foreground">
        {t('likes.reveal.description')}
      </p>
    </div>
  )
}
