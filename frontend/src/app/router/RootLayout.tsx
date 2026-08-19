import { Outlet } from '@tanstack/react-router'

/** Корневая обёртка всех экранов: безопасные зоны Telegram и высота вьюпорта. */
export function RootLayout() {
  return (
    <div className="flex min-h-viewport flex-col pt-safe pb-safe">
      <Outlet />
    </div>
  )
}
