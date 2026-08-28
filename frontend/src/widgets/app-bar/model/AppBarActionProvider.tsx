import { useState, type ReactNode } from 'react'

import {
  AppBarActionContext,
  AppBarActionSetterContext,
  type AppBarAction,
} from './app-bar-action-context'

type AppBarActionProviderProps = {
  children: ReactNode
}

/** Хранит действие шапки, которое задаёт текущий экран. */
export function AppBarActionProvider({ children }: AppBarActionProviderProps) {
  const [action, setAction] = useState<AppBarAction | null>(null)

  return (
    <AppBarActionSetterContext.Provider value={setAction}>
      <AppBarActionContext.Provider value={action}>{children}</AppBarActionContext.Provider>
    </AppBarActionSetterContext.Provider>
  )
}
