import { Outlet, useRouterState } from '@tanstack/react-router'

import { ROUTES } from '@/shared/config'
import { cn } from '@/shared/lib'
import { AppBar, AppBarActionProvider } from '@/widgets/app-bar'
import { TabBar } from '@/widgets/tab-bar'

/** Экраны с нижним меню. Вход, анкета и документы показываются без него. */
const TABBED_ROUTES: readonly string[] = [
  ROUTES.feed,
  ROUTES.likes,
  ROUTES.matches,
  ROUTES.matchHub,
  ROUTES.ideas,
  ROUTES.profile,
  ROUTES.profileWallet,
]

/**
 * Корневая обёртка всех экранов: безопасные зоны Telegram и высота вьюпорта.
 * Ширина ограничена: мини-апп рисуется в узком окне, и на большом экране
 * (Telegram Desktop, браузер при отладке) он должен выглядеть так же.
 *
 * Обвязка (шапка и нижнее меню) живёт здесь, а не на экранах: иначе она
 * перемонтировалась бы при каждом переходе между вкладками и дублировалась
 * в пяти файлах.
 *
 * По макетам обвязка лежит поверх контента, а не занимает место в потоке:
 * стекло размывает то, что под ним, и без перекрытия этот приём не виден.
 * Поэтому отступы под неё даёт сам контент (`pt-chrome`/`pb-chrome`), а не
 * панель, а панели прибиты к окну (`fixed`).
 *
 * Прокручивается сама страница, а не внутренний блок с `overflow-y: auto`.
 * Это важно именно внутри Telegram: клиент разбирает вертикальный жест до
 * веб-страницы и отдаёт его ей, только когда прокручивается документ —
 * с внутренним контейнером экраны длиннее окна (профиль, анкета) просто
 * не листались. Побочный выигрыш — нативная инерция прокрутки на iOS.
 */
export function RootLayout() {
  // Сравниваем с id совпавшего роута, а не с `pathname`: у хаба мэтча путь
  // содержит id (`/matches/7c5f…`) и в список шаблонов никогда не попадёт.
  const routeId = useRouterState({ select: (state) => state.matches.at(-1)?.routeId })
  const withTabs = routeId !== undefined && TABBED_ROUTES.includes(routeId)

  return (
    <AppBarActionProvider>
      <div className="mx-auto flex min-h-viewport w-full max-w-app flex-col bg-background">
        {withTabs && <AppBar />}

        <div
          className={cn(
            'flex min-h-0 flex-1 flex-col',
            withTabs ? 'pt-chrome pb-chrome' : 'pt-safe pb-safe',
          )}
        >
          <Outlet />
        </div>

        {withTabs && <TabBar />}
      </div>
    </AppBarActionProvider>
  )
}
