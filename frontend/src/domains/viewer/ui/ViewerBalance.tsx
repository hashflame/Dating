import { Star } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { cn } from '@/shared/lib'
import { Skeleton } from '@/shared/ui'

import { useViewer } from '../api/use-viewer'

type ViewerBalanceProps = {
  className?: string
}

/**
 * Баланс зорок текущего пользователя.
 *
 * По макетам это «пилюля» в углу шапки: звезда и число. Слово («зорка»,
 * «зоркі», «зорак») в неё не помещается и на каждом значении меняло бы
 * ширину панели, поэтому оно ушло в подпись для читалок — там формы слова
 * по числу по-прежнему даёт i18n.
 */
export function ViewerBalance({ className }: ViewerBalanceProps) {
  const { t } = useTranslation()
  const { data: viewer, isPending, isError } = useViewer()

  // Элемент строчный, поэтому вместо EmptyState/ErrorState — скелетон и короткий текст.
  if (isPending) {
    return <Skeleton className={cn('h-9 w-16 rounded-full', className)} />
  }

  if (isError) {
    return (
      <span className={cn('px-2 text-sm text-destructive', className)}>{t('state.error')}</span>
    )
  }

  return (
    <span
      className={cn(
        'inline-flex h-9 items-center gap-1.5 rounded-full bg-amber/15 px-3 text-sm font-bold text-foreground',
        className,
      )}
      aria-label={t('viewer.balance', { count: viewer.sparksBalance })}
    >
      <Star className="size-4 fill-amber text-amber" aria-hidden />
      <span aria-hidden>{viewer.sparksBalance}</span>
    </span>
  )
}
