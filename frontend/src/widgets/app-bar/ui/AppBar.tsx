import { ViewerBalance } from '@/domains/viewer'

/**
 * Верхняя панель разделов с нижним меню: одна и та же на всех вкладках.
 *
 * Живёт в обёртке роутера, а не на экранах: раньше баланс зорок рисовала
 * только лента, и при переходе на другую вкладку он исчезал — выглядело как
 * будто зорки потерялись.
 */
export function AppBar() {
  return (
    <header className="flex min-h-11 shrink-0 items-center justify-between gap-3 px-4">
      <ViewerBalance />
    </header>
  )
}
