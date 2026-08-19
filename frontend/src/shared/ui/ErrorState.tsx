import { TriangleAlert } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { cn } from '@/shared/lib'

import { Button } from './kit/button'

type ErrorStateProps = {
  title?: string
  description?: string
  /** Если передан — показывается кнопка «Повторить». */
  onRetry?: () => void
  className?: string
}

/** Состояние ошибки. Тексты API не переведены, поэтому наружу их не показываем. */
export function ErrorState({ title, description, onRetry, className }: ErrorStateProps) {
  const { t } = useTranslation()

  return (
    <div
      className={cn(
        'flex flex-col items-center justify-center gap-3 px-6 py-12 text-center',
        className,
      )}
    >
      <TriangleAlert className="size-10 text-destructive" aria-hidden />
      <p className="font-medium text-foreground">{title ?? t('state.error')}</p>
      {description && <p className="text-sm text-muted-foreground">{description}</p>}
      {onRetry && (
        <Button variant="secondary" size="sm" onClick={onRetry}>
          {t('action.retry')}
        </Button>
      )}
    </div>
  )
}
