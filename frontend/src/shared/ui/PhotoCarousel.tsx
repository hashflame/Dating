import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { cn } from '@/shared/lib'

type PhotoCarouselProps = {
  urls: string[]
  /** Описание для читалок: чьи это фото. */
  label: string
  className?: string
}

/**
 * Фото анкеты с точками и переключением тапом по половинам кадра —
 * как в дека ленты и в шторке полной анкеты.
 *
 * Битую ссылку не показываем сломанной картинкой: подставляем градиент.
 * Это не только про баг стенда с URL фото — в мобильной сети запрос может
 * не дойти, и пустой кадр выглядит лучше иконки «нет изображения».
 */
export function PhotoCarousel({ urls, label, className }: PhotoCarouselProps) {
  const { t } = useTranslation()
  const [index, setIndex] = useState(0)
  const [failed, setFailed] = useState<Record<number, boolean>>({})

  const total = urls.length
  const current = Math.min(index, Math.max(total - 1, 0))
  const url = urls[current]

  const go = (step: number): void => {
    setIndex((value) => (value + step + total) % total)
  }

  return (
    <div className={cn('relative overflow-hidden bg-gradient-photo-1', className)}>
      {url && !failed[current] && (
        <img
          src={url}
          alt={label}
          loading="lazy"
          decoding="async"
          onError={() => setFailed((value) => ({ ...value, [current]: true }))}
          className="size-full object-cover"
        />
      )}

      {total > 1 && (
        <>
          <button
            type="button"
            onClick={() => go(-1)}
            aria-label={t('photo.previous')}
            className="absolute inset-y-0 left-0 w-1/3 outline-none"
          />
          <button
            type="button"
            onClick={() => go(1)}
            aria-label={t('photo.next')}
            className="absolute inset-y-0 right-0 w-1/3 outline-none"
          />

          <div className="pointer-events-none absolute inset-x-3 top-3 flex gap-1" aria-hidden>
            {urls.map((_, dot) => (
              <span
                key={dot}
                className={cn(
                  'h-0.5 flex-1 rounded-full transition-colors',
                  dot === current ? 'bg-white' : 'bg-white/35',
                )}
              />
            ))}
          </div>
        </>
      )}
    </div>
  )
}
