import { Outlet, useRouterState } from '@tanstack/react-router'

import { ROUTES } from '@/shared/config'
import { AppBar } from '@/widgets/app-bar'
import { TabBar } from '@/widgets/tab-bar'

/** Экраны с нижним меню. Вход, анкета и документы показываются без него. */
const TABBED_ROUTES: readonly string[] = [
  ROUTES.feed,
  ROUTES.likes,
  ROUTES.matches,
  ROUTES.matchHub,
  ROUTES.matchQuestion,
  ROUTES.ideas,
  ROUTES.profile,
  ROUTES.profileWallet,
  ROUTES.profileInterests,
  ROUTES.profileDatePrefs,
]

/**
 * Корневая обёртка всех экранов: безопасные зоны Telegram и высота вьюпорта.
 * Ширина ограничена: мини-апп рисуется в узком окне, и на большом экране
 * (Telegram Desktop, браузер при отладке) он должен выглядеть так же.
 *
 * Нижнее меню живёт здесь, а не на экранах: иначе оно перемонтировалось бы
 * при каждом переходе между вкладками и дублировалось в пяти файлах.
 * Отступ снизу тоже тут: с меню его даёт само меню, без меню — обёртка.
 */
export function RootLayout() {
  // Сравниваем с id совпавшего роута, а не с `pathname`: у хаба мэтча путь
  // содержит id (`/matches/7c5f…`) и в список шаблонов никогда не попадёт.
  const routeId = useRouterState({ select: (state) => state.matches.at(-1)?.routeId })
  const withTabs = routeId !== undefined && TABBED_ROUTES.includes(routeId)

  return (
    <div className="flex min-h-viewport justify-center bg-background">
      <div className="flex h-viewport w-full max-w-app flex-col overflow-hidden pt-safe">
        {withTabs && <AppBar />}

        <div className="flex min-h-0 flex-1 flex-col overflow-y-auto">
          <Outlet />
        </div>

        {withTabs ? <TabBar /> : <div className="pb-safe" />}
      </div>
    </div>
  )
}
