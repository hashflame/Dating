import { Skeleton } from '@/shared/ui'

/** Заглушка шага, пока грузится черновик анкеты. */
export function OnboardingStepSkeleton() {
  return (
    <div className="flex flex-1 flex-col gap-4 px-5 pt-8">
      <Skeleton className="h-8 w-2/3" />
      <Skeleton className="h-11 w-full" />
      <Skeleton className="h-11 w-full" />
      <Skeleton className="h-11 w-full" />
    </div>
  )
}
