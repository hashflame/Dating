import { Heart, Undo2, X } from 'lucide-react'
import { type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { cn } from '@/shared/lib'
import { Button } from '@/shared/ui'

type SwipeActionsProps = {
  onDislike: () => void
  onLike: () => void
  onUndo: () => void
  /** Отмены кончились или свайпов ещё не было — кнопка неактивна, но видна. */
  canUndo: boolean
  disabled: boolean
}

/**
 * Ряд действий под карточкой (S-10).
 *
 * «Мимо» и «нравится» — равноправная пара в центре: это симметричный выбор,
 * и подталкивать к одному из них размером неправильно. Слева отмена — она
 * служебная и потому мельче и без цвета.
 *
 * Фильтры переехали в шапку (макет «Дека»): в ряду они читались как третье
 * решение по анкете, хотя к текущему человеку отношения не имеют.
 *
 * Ни теней, ни цветных ореолов: под залитым кругом цветная тень читается
 * грязным нимбом, а не светом. Кнопки держатся одной заливкой.
 */
export function SwipeActions({ onDislike, onLike, onUndo, canUndo, disabled }: SwipeActionsProps) {
  const { t } = useTranslation()

  return (
    // Сетка, а не строка: пара «мимо/нравится» обязана стоять по центру
    // карточки, а отмена — сбоку. В обычном flex-ряду отмена сдвигала бы пару
    // вправо, и симметрия выбора ломалась.
    <div className="grid grid-cols-[3rem_1fr_3rem] items-center gap-3">
      <ActionButton
        onClick={onUndo}
        disabled={disabled || !canUndo}
        label={t('feed.action.undo')}
        className="size-12 bg-surface text-muted-foreground hover:bg-surface-strong"
      >
        <Undo2 className="size-5" aria-hidden />
      </ActionButton>

      <div className="flex items-center justify-center gap-5">
        <ActionButton
          onClick={onDislike}
          disabled={disabled}
          label={t('feed.action.dislike')}
          className="size-18 bg-destructive text-destructive-foreground hover:bg-destructive/90"
        >
          <X className="size-8 stroke-[2.5]" aria-hidden />
        </ActionButton>

        <ActionButton
          onClick={onLike}
          disabled={disabled}
          label={t('feed.action.like')}
          className="size-18 bg-moss text-moss-foreground hover:bg-moss/90"
        >
          <Heart className="size-8 fill-current" aria-hidden />
        </ActionButton>
      </div>
    </div>
  )
}

type ActionButtonProps = {
  onClick: () => void
  disabled: boolean
  label: string
  className: string
  children: ReactNode
}

function ActionButton({ onClick, disabled, label, className, children }: ActionButtonProps) {
  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={onClick}
      disabled={disabled}
      aria-label={label}
      className={cn('rounded-full', className)}
    >
      {children}
    </Button>
  )
}
