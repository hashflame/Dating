import { LoaderCircle } from 'lucide-react'

import { cn } from '@/shared/lib'

type SpinnerProps = {
  className?: string
}

/** Индикатор загрузки. Для скелетонов контента используй `Skeleton`. */
export function Spinner({ className }: SpinnerProps) {
  return (
    <LoaderCircle
      className={cn('size-5 animate-spin text-muted-foreground', className)}
      aria-hidden
    />
  )
}
