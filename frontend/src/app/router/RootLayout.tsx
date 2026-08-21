import { Outlet } from '@tanstack/react-router'

/**
 * Корневая обёртка всех экранов: безопасные зоны Telegram и высота вьюпорта.
 * Ширина ограничена: мини-апп рисуется в узком окне, и на большом экране
 * (Telegram Desktop, браузер при отладке) он должен выглядеть так же.
 */
export function RootLayout() {
  return (
    <div className="flex min-h-viewport justify-center bg-background">
      <div className="flex w-full max-w-[32rem] flex-col pt-safe pb-safe">
        <Outlet />
      </div>
    </div>
  )
}
