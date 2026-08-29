import { Heart, X } from 'lucide-react'
import { motion, useMotionValue, useTransform, type PanInfo } from 'motion/react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { type SwipeAction } from '@/domains/feed'
import { cn } from '@/shared/lib'

type Decision = Extract<SwipeAction, 'like' | 'dislike'>

type SwipeDeckProps = {
  /** Меняется вместе с карточкой — по нему сбрасывается позиция после свайпа. */
  cardId: string
  /**
   * Отправляет решение. Возвращает `false`, если сервер отказал: улетевшая
   * карточка тогда возвращается на место, а не оставляет пустой экран.
   */
  onSwipe: (action: Decision) => Promise<boolean>
  disabled: boolean
  /** Карточка ленты — вместе с кнопками, которые лежат на ней. */
  children: React.ReactNode
}

/** Сколько нужно увести карточку, чтобы свайп сработал. */
const COMMIT_DISTANCE = 130
/** Резкий бросок считаем свайпом и раньше — но не с места. */
const COMMIT_VELOCITY = 700
/** Меньше этого бросок не считается: столько палец проходит и при случайном касании. */
const FLICK_MIN_DISTANCE = 55
/** С этого расстояния проявляется подсказка «нравится»/«мимо». */
const HINT_FROM = 30
/** Куда карточка улетает при подтверждённом свайпе. */
const FLY_OUT_DISTANCE = 600

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
 *
 * Карточка пересоздаётся по `cardId`: позиция, наклон и состояние отлёта
 * живут внутри неё, и новая анкета всегда начинается по центру.
 */
export function SwipeDeck({ cardId, onSwipe, disabled, children }: SwipeDeckProps) {
  return (
    <DeckCard key={cardId} onSwipe={onSwipe} disabled={disabled}>
      {children}
    </DeckCard>
  )
}

type DeckCardProps = Omit<SwipeDeckProps, 'cardId'>

function DeckCard({ onSwipe, disabled, children }: DeckCardProps) {
  const { t } = useTranslation()

  const x = useMotionValue(0)
  const rotate = useTransform(x, [-FLY_OUT_DISTANCE, FLY_OUT_DISTANCE], [-18, 18])
  const likeOpacity = useTransform(x, [HINT_FROM, COMMIT_DISTANCE], [0, 1])
  const dislikeOpacity = useTransform(x, [-COMMIT_DISTANCE, -HINT_FROM], [1, 0])

  /** Карточка улетает: решение принято, ждём ответ сервера. `null` — на месте. */
  const [flying, setFlying] = useState<Decision | null>(null)

  /**
   * Отпустили — считаем, свайп это или возврат.
   *
   * Только расстояния мало: медленное короткое движение тоже должно
   * засчитываться, если палец бросили. Но и одной скорости мало — при возврате
   * пальца назад скорость высокая, а решение принимать нельзя. Поэтому бросок
   * засчитывается, лишь когда он совпадает по направлению со смещением и
   * карточка успела заметно сдвинуться: «начал и передумал» остаётся возвратом.
   */
  const handleDragEnd = (_event: unknown, info: PanInfo): void => {
    const offset = info.offset.x
    const direction = Math.sign(offset)
    if (direction === 0 || flying !== null) return

    const flicked =
      Math.sign(info.velocity.x) === direction &&
      Math.abs(info.velocity.x) > COMMIT_VELOCITY &&
      Math.abs(offset) > FLICK_MIN_DISTANCE

    if (Math.abs(offset) < COMMIT_DISTANCE && !flicked) return

    commit(direction > 0 ? 'like' : 'dislike')
  }

  const commit = (action: Decision): void => {
    setFlying(action)

    void onSwipe(action).then((accepted) => {
      // Сервер отказал — возвращаем карточку: следующей анкеты не будет,
      // и пустой экран человек прочитает как потерянного собеседника.
      if (!accepted) setFlying(null)
    })
  }

  return (
    <motion.div
      drag={disabled || flying !== null ? false : 'x'}
      dragSnapToOrigin
      // Единица — движение один в один с пальцем. При меньшем значении
      // карточка отставала от жеста, и было неясно, зачтётся он или нет.
      dragElastic={1}
      dragConstraints={{ left: 0, right: 0 }}
      dragTransition={{ bounceStiffness: 600, bounceDamping: 45 }}
      onDragEnd={handleDragEnd}
      style={{ x, rotate }}
      // Анимация только на отлёте. Появление новой карточки не анимируем
      // намеренно: свёрнутый мини-апп замораживает анимации, и «проявление»
      // могло бы застыть на полупрозрачной карточке.
      animate={
        flying === null
          ? undefined
          : { x: flying === 'like' ? FLY_OUT_DISTANCE : -FLY_OUT_DISTANCE, opacity: 0 }
      }
      transition={{ duration: 0.22, ease: 'easeOut' }}
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
