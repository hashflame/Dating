import { cn } from '@/shared/lib'

type ProgressBarProps = {
  /** Прогресс 0–100. Не передан — бесконечная полоса «идёт загрузка». */
  value?: number
  className?: string
}

/** Тонкая полоса прогресса. Без `value` показывает неопределённую загрузку. */
export function ProgressBar({ value, className }: ProgressBarProps) {
  const isIndeterminate = value === undefined

  return (
    <div
      className={cn('h-[5px] overflow-hidden rounded-full bg-tag', className)}
      role="progressbar"
      aria-valuenow={isIndeterminate ? undefined : value}
      aria-valuemin={0}
      aria-valuemax={100}
    >
      <div
        className={cn(
          'h-full rounded-full bg-brand',
          isIndeterminate
            ? 'w-1/3 motion-safe:animate-progress-slide motion-reduce:w-full'
            : 'transition-[width] duration-300',
        )}
        style={isIndeterminate ? undefined : { width: `${String(value)}%` }}
      />
    </div>
  )
}
