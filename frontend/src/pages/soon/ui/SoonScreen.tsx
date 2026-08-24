import { type LucideIcon } from 'lucide-react'

import { EmptyState } from '@/shared/ui'

type SoonScreenProps = {
  title: string
  description: string
  icon: LucideIcon
}

/**
 * Раздел ещё не сделан.
 *
 * Экран нужен уже сейчас: на разделы ведут вкладки нижнего меню, и пустой
 * экран без объяснения выглядит как поломка. Чего не хватает от API для
 * каждого раздела — в docs/api-gaps.md.
 */
export function SoonScreen({ title, description, icon }: SoonScreenProps) {
  return (
    <main className="flex flex-1 items-center justify-center">
      <EmptyState icon={icon} title={title} description={description} />
    </main>
  )
}
