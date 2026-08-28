import { Heart, X } from 'lucide-react'
import { motion, useMotionValue, useTransform, type PanInfo } from 'motion/react'
import { useTranslation } from 'react-i18next'

import { type SwipeAction } from '@/domains/feed'
import { cn } from '@/shared/lib'

type SwipeDeckProps = {
  /** Меняется вместе с карточкой — по нему сбрасывается позиция после свайпа. */
  cardId: string
  onSwipe: (action: Extract<SwipeAction, 'like' | 'dislike'>) => void
  disabled: boolean
  /** Карточка ленты — вместе с кнопками, которые лежат на ней. */
  children: React.ReactNode
}

/** Сколько нужно увести карточку, чтобы свайп сработал. */
const COMMIT_DISTANCE = 110
/** Резкий бросок считаем свайпом даже на коротком расстоянии. */
const COMMIT_VELOCITY = 500

/**
 * Перетаскивание деки: вправо — «нравится», влево — «мимо».
 *
 * Лежит в странице, а не в домене: единственная тяжёлая зависимость проекта
 * (motion, ~100 kB) не должна попадать в барель домена — иначе её затянет
 * любой, кто импортирует домен, включая панель разработки в главном чанке.
 *
 * Кнопки лежат на самой карточке, поэтому при свайпе уезжает и наклоняется
 * всё разом, как один предмет. Когда они стояли отдельным блоком под декой,
 * связь «эта кнопка про этого человека» распадалась ровно в тот момент,
 * когда принимается решение.
 *
 * Кнопки делают то же, что жест, и остаются основным способом: свайп
 * не обязателен и недоступен с клавиатуры. Пока деку тянут, поверх
 * проявляется подсказка, что произойдёт при отпускании.
 */
export function SwipeDeck({ cardId, onSwipe, disabled, children }: SwipeDeckProps) {
  const { t } = useTranslation()

  const x = useMotionValue(0)
  const rotate = useTransform(x, [-200, 200], [-12, 12])
  const likeOpacity = useTransform(x, [40, COMMIT_DISTANCE], [0, 1])
  const dislikeOpacity = useTransform(x, [-COMMIT_DISTANCE, -40], [1, 0])

  const handleDragEnd = (_event: unknown, info: PanInfo): void => {
    const passed =
      Math.abs(info.offset.x) > COMMIT_DISTANCE || Math.abs(info.velocity.x) > COMMIT_VELOCITY

    if (!passed) return

    onSwipe(info.offset.x > 0 ? 'like' : 'dislike')
  }

  return (
    <motion.div
      // Ключ по карточке: новая анкета появляется по центру, а не там,
      // где осталась предыдущая.
      key={cardId}
      drag={disabled ? false : 'x'}
      dragSnapToOrigin
      dragElastic={0.5}
      dragConstraints={{ left: 0, right: 0 }}
      onDragEnd={handleDragEnd}
      style={{ x, rotate }}
      // Карточка занимает деку целиком — фото тянется до нижнего меню.
      className="relative flex min-h-0 flex-1 touch-pan-y flex-col"
    >
      {children}

      <Hint opacity={likeOpacity} label={t('feed.action.like')} className="left-4 text-moss">
        <Heart className="size-5" aria-hidden />
      </Hint>

      <Hint
        opacity={dislikeOpacity}
        label={t('feed.action.dislike')}
        className="right-4 text-destructive"
      >
        <X className="size-5" aria-hidden />
      </Hint>
    </motion.div>
  )
}

type HintProps = {
  opacity: ReturnType<typeof useTransform<number, number>>
  label: string
  className: string
  children: React.ReactNode
}

function Hint({ opacity, label, className, children }: HintProps) {
  return (
    <motion.span
      style={{ opacity }}
      aria-hidden
      className={cn(
        // Подложка почти непрозрачная и одинаковая в обеих темах: подсказка
        // лежит на фото, а не на фоне экрана, и тонировать её темой нельзя.
        'pointer-events-none absolute top-4 flex items-center gap-1.5 rounded-full bg-brand-foreground/90 px-3.5 py-2 text-sm font-bold uppercase backdrop-blur',
        className,
      )}
    >
      {children}
      {label}
    </motion.span>
  )
}
