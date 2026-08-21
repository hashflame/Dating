import { cn } from '@/shared/lib'

type StepDotsProps = {
  /** Номер текущего шага, начиная с 1. */
  current: number
  total: number
}

/** Индикатор шагов анкеты: пройденные и текущий — фирменным цветом. */
export function StepDots({ current, total }: StepDotsProps) {
  return (
    <div
      className="flex justify-center gap-1.5"
      aria-label={`Шаг ${String(current)} из ${String(total)}`}
    >
      {Array.from({ length: total }, (_, index) => (
        <span
          key={index}
          className={cn(
            'h-1.5 rounded-full transition-all',
            index < current ? 'w-5 bg-brand' : 'w-1.5 bg-border',
          )}
        />
      ))}
    </div>
  )
}
