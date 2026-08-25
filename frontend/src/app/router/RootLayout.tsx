import { Outlet, useRouterState } from '@tanstack/react-router'

import { ROUTES } from '@/shared/config'
import { AppBar } from '@/widgets/app-bar'
import { TabBar } from '@/widgets/tab-bar'

/** Экраны с нижним меню. Вход, анкета и документы показываются без него. */
const TABBED_ROUTES: readonly string[] = [
  ROUTES.feed,
  ROUTES.likes,
  ROUTES.matches,
  ROUTES.ideas,
  ROUTES.profile,
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
  const pathname = useRouterState({ select: (state) => state.location.pathname })
  const withTabs = TABBED_ROUTES.includes(pathname)

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
