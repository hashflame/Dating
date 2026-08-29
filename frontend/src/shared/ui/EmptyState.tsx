import { Inbox } from 'lucide-react'
import { type ComponentType, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { cn } from '@/shared/lib'

type EmptyStateProps = {
  icon?: ComponentType<{ className?: string }>
  title?: string
  description?: string
  /** Кнопка или ссылка — что пользователь может сделать вместо ожидания. */
  action?: ReactNode
  className?: string
}

/** Пустое состояние списка или экрана. */
export function EmptyState({
  icon: Icon = Inbox,
  title,
  description,
  action,
  className,
}: EmptyStateProps) {
  const { t } = useTranslation()

  return (
    <div
      className={cn(
        'flex flex-col items-center justify-center gap-3 px-6 py-12 text-center',
        className,
      )}
    >
      <Icon className="size-10 text-muted-foreground" aria-hidden />
      <p className="text-lg font-bold text-foreground">{title ?? t('state.empty')}</p>
      {description && <p className="text-sm text-muted-foreground">{description}</p>}
      {action}
    </div>
  )
}
