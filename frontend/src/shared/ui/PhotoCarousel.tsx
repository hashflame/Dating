import { useRef, useState, type PointerEvent as ReactPointerEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { cn } from '@/shared/lib'

type PhotoCarouselProps = {
  urls: string[]
  /** Описание для читалок: чьи это фото. */
  label: string
  /**
   * Листать ли фото горизонтальным жестом. Выключается там, где этот же жест
   * уже занят: в деке ленты горизонтальная протяжка — это лайк или «мимо».
   */
  swipeable?: boolean
  className?: string
}

/** Столько нужно провести пальцем, чтобы это считалось листанием, а не тапом. */
const SWIPE_MIN_DISTANCE = 40

/**
 * Фото анкеты с точками и переключением тапом по половинам кадра —
 * как в дека ленты и в шторке полной анкеты.
 *
 * Кроме тапа фото листается горизонтальным жестом: тапом по краю кадра
 * пользуются те, кто про него знает, а пробуют все сначала свайп. Жест
 * отключается там, где он уже занят решением по анкете (`swipeable`).
 *
 * Битую ссылку не показываем сломанной картинкой: подставляем градиент.
 * Это не только про баг стенда с URL фото — в мобильной сети запрос может
 * не дойти, и пустой кадр выглядит лучше иконки «нет изображения».
 */
export function PhotoCarousel({ urls, label, swipeable = true, className }: PhotoCarouselProps) {
  const { t } = useTranslation()
  const [index, setIndex] = useState(0)
  const [failed, setFailed] = useState<Record<number, boolean>>({})

  const total = urls.length
  const current = Math.min(index, Math.max(total - 1, 0))
  const url = urls[current]

  const start = useRef<{ x: number; y: number } | null>(null)
  /**
   * Жест закончился листанием — значит, `click` по прозрачной кнопке-половине,
   * который браузер пошлёт следом, уже не тап и листать второй раз не должен.
   */
  const swiped = useRef(false)

  const go = (step: number): void => {
    setIndex((value) => (value + step + total) % total)
  }

  /** Тап по половине кадра. После жеста-листания клик глотаем. */
  const tap = (step: number): void => {
    if (swiped.current) {
      swiped.current = false
      return
    }

    go(step)
  }

  const handlePointerDown = (event: ReactPointerEvent<HTMLDivElement>): void => {
    swiped.current = false
    start.current = { x: event.clientX, y: event.clientY }
  }

  const handlePointerUp = (event: ReactPointerEvent<HTMLDivElement>): void => {
    const from = start.current
    start.current = null
    if (from === null) return

    const dx = event.clientX - from.x
    const dy = event.clientY - from.y

    // Вертикальную составляющую отсекаем: под пальцем ещё и прокрутка шторки,
    // и наклонное движение не должно листать фото.
    if (Math.abs(dx) < SWIPE_MIN_DISTANCE || Math.abs(dx) <= Math.abs(dy)) return

    swiped.current = true
    go(dx < 0 ? 1 : -1)
  }

  const gestures =
    swipeable && total > 1
      ? {
          onPointerDown: handlePointerDown,
          onPointerUp: handlePointerUp,
          onPointerCancel: () => {
            start.current = null
          },
        }
      : {}

  return (
    <div
      className={cn('relative overflow-hidden bg-gradient-photo-1', className)}
      {...gestures}
      // Вертикальную прокрутку оставляем браузеру, горизонталь разбираем сами.
      style={swipeable && total > 1 ? { touchAction: 'pan-y' } : undefined}
    >
      {url && !failed[current] && (
        <img
          src={url}
          alt={label}
          loading="lazy"
          decoding="async"
          draggable={false}
          onError={() => setFailed((value) => ({ ...value, [current]: true }))}
          className="size-full object-cover select-none"
        />
      )}

      {total > 1 && (
        <>
          <button
            type="button"
            onClick={() => tap(-1)}
            aria-label={t('photo.previous')}
            className="absolute inset-y-0 left-0 w-1/3 outline-none"
          />
          <button
            type="button"
            onClick={() => tap(1)}
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
