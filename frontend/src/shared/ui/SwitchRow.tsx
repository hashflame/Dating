import { useId } from 'react'

import { cn } from '@/shared/lib'
import { Switch } from '@/shared/ui/kit/switch'

type SwitchRowProps = {
  label: string
  checked: boolean
  onCheckedChange: (checked: boolean) => void
  /** Пояснение под подписью. */
  hint?: string
  className?: string
}

/**
 * Строка «подпись — тумблер». Одна на все списки настроек: фильтры ленты,
 * приватность, уведомления. По подписи тоже можно нажать.
 */
export function SwitchRow({ label, checked, onCheckedChange, hint, className }: SwitchRowProps) {
  const id = useId()

  return (
    <div className={cn('flex min-h-11 items-center justify-between gap-4', className)}>
      <label htmlFor={id} className="cursor-pointer">
        <span className="block text-base text-foreground">{label}</span>
        {hint && <span className="block text-tiny text-faint">{hint}</span>}
      </label>

      <Switch id={id} checked={checked} onCheckedChange={onCheckedChange} className="shrink-0" />
    </div>
  )
}
