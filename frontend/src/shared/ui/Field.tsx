import { type ReactNode } from 'react'

import { cn } from '@/shared/lib'

type FieldProps = {
  label: string
  children: ReactNode
  /** Пояснение под полем. Скрывается, когда есть ошибка. */
  hint?: string
  error?: string
  /** Подпись справа от основной — «можно выбрать до двух». */
  aside?: string
  htmlFor?: string
  className?: string
}

/**
 * Поле из макетов: подпись-рубрика капсом вразрядку, контрол, под ним
 * пояснение или ошибка. Разрядка и вес держат подпись читаемой на мелком
 * кегле — без них капс сливается в серую полосу.
 */
export function Field({ label, children, hint, error, aside, htmlFor, className }: FieldProps) {
  return (
    <div className={cn('flex flex-col gap-2.5', className)}>
      <div className="flex items-center justify-between gap-2">
        <label htmlFor={htmlFor} className="text-eyebrow font-bold text-muted-foreground uppercase">
          {label}
        </label>
        {aside && <span className="text-tiny font-semibold text-muted-foreground">{aside}</span>}
      </div>

      {children}

      {error ? (
        <span className="text-tiny text-destructive">{error}</span>
      ) : (
        hint && <span className="text-tiny text-faint">{hint}</span>
      )}
    </div>
  )
}
