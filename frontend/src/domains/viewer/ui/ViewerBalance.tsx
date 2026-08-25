import { Star } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { cn } from '@/shared/lib'
import { Skeleton } from '@/shared/ui'

import { useViewer } from '../api/use-viewer'

type ViewerBalanceProps = {
  className?: string
}

/** Баланс зорок текущего пользователя. Формы слова даёт i18n по числу. */
export function ViewerBalance({ className }: ViewerBalanceProps) {
  const { t } = useTranslation()
  const { data: viewer, isPending, isError } = useViewer()

  // Элемент строчный, поэтому вместо EmptyState/ErrorState — скелетон и короткий текст.
  if (isPending) {
    return <Skeleton className={cn('h-5 w-20', className)} />
  }

  if (isError) {
    return <span className={cn('text-sm text-destructive', className)}>{t('state.error')}</span>
  }

  return (
    <span className={cn('inline-flex items-center gap-1.5 text-sm text-foreground', className)}>
      <Star className="size-4 text-brand" aria-hidden />
      {t('viewer.balance', { count: viewer.sparksBalance })}
    </span>
  )
}
