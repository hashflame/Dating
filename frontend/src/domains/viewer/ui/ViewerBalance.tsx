import { useTranslation } from 'react-i18next'

import { cn } from '@/shared/lib'
import { Skeleton, SparkIcon } from '@/shared/ui'

import { useViewer } from '../api/use-viewer'

type ViewerBalanceProps = {
  className?: string
}

/**
 * Баланс зорок текущего пользователя.
 *
 * По макетам это «пилюля» в углу шапки: в одну строку зорка, подпись и число.
 * Само слово («зорка», «зоркі», «зорак») в неё не помещается и на каждом
 * значении меняло бы ширину панели, поэтому в пилюле стоит неизменная подпись
 * «Баланс» — она же отделяет наши зорки от Telegram Stars, — а формы слова по
 * числу остались в подписи для читалок.
 */
export function ViewerBalance({ className }: ViewerBalanceProps) {
  const { t } = useTranslation()
  const { data: viewer, isPending, isError } = useViewer()

  // Элемент строчный, поэтому вместо EmptyState/ErrorState — скелетон и короткий текст.
  if (isPending) {
    return <Skeleton className={cn('h-9 w-28 rounded-full', className)} />
  }

  if (isError) {
    return (
      <span className={cn('px-2 text-sm text-destructive', className)}>{t('state.error')}</span>
    )
  }

  return (
    <span
      className={cn(
        'inline-flex h-9 items-center gap-1.5 rounded-full bg-spark/15 px-3',
        className,
      )}
      aria-label={t('viewer.balance', { count: viewer.sparksBalance })}
    >
      <SparkIcon />
      <span className="text-eyebrow font-semibold text-muted-foreground uppercase" aria-hidden>
        {t('viewer.balanceLabel')}
      </span>
      <span className="text-sm font-bold text-foreground" aria-hidden>
        {viewer.sparksBalance}
      </span>
    </span>
  )
}
