import { Heart, SlidersHorizontal, Undo2, X } from 'lucide-react'
import { type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { cn } from '@/shared/lib'
import { Button } from '@/shared/ui'

type SwipeActionsProps = {
  onDislike: () => void
  onLike: () => void
  onUndo: () => void
  onOpenFilters: () => void
  /** Отмены кончились или свайпов ещё не было — кнопка неактивна, но видна. */
  canUndo: boolean
  disabled: boolean
}

/**
 * Ряд действий под карточкой (S-10).
 *
 * «Мимо» и «нравится» — равноправная пара в центре: это симметричный выбор,
 * и подталкивать к одному из них размером неправильно. По краям — отмена
 * и фильтры, они мельче и служебные.
 */
export function SwipeActions({
  onDislike,
  onLike,
  onUndo,
  onOpenFilters,
  canUndo,
  disabled,
}: SwipeActionsProps) {
  const { t } = useTranslation()

  return (
    <div className="grid grid-cols-[2.75rem_1fr_2.75rem] items-center gap-3">
      <ActionButton
        onClick={onUndo}
        disabled={disabled || !canUndo}
        label={t('feed.action.undo')}
        className="size-11"
      >
        <Undo2 className="size-5" aria-hidden />
      </ActionButton>

      <div className="flex items-center justify-center gap-4">
        <ActionButton
          onClick={onDislike}
          disabled={disabled}
          label={t('feed.action.dislike')}
          className="size-16 border-destructive bg-destructive text-destructive-foreground hover:bg-destructive/90"
        >
          <X className="size-7" aria-hidden />
        </ActionButton>

        <ActionButton
          onClick={onLike}
          disabled={disabled}
          label={t('feed.action.like')}
          className="size-16 border-moss bg-moss text-moss-foreground hover:bg-moss/90"
        >
          <Heart className="size-7" aria-hidden />
        </ActionButton>
      </div>

      <ActionButton
        onClick={onOpenFilters}
        disabled={false}
        label={t('feed.action.filters')}
        className="size-11"
      >
        <SlidersHorizontal className="size-5" aria-hidden />
      </ActionButton>
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
      variant="outline"
      size="icon"
      onClick={onClick}
      disabled={disabled}
      aria-label={label}
      className={cn('rounded-full border-border bg-card shadow-sm', className)}
    >
      {children}
    </Button>
  )
}
