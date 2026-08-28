import { type LucideIcon } from 'lucide-react'
import { createContext, useContext, useEffect, useMemo } from 'react'

/** Кнопка, которую экран отдаёт в левый угол шапки. */
export type AppBarAction = {
  Icon: LucideIcon
  /** Подпись для читалок: у кнопки только иконка. */
  label: string
  onClick: () => void
}

/**
 * Действие шапки, которое задаёт текущий экран.
 *
 * В макетах кнопка фильтров переехала из ряда под декой в левый угол шапки,
 * а шапка живёт в обёртке роутера — иначе баланс зорок перемонтировался бы
 * на каждом переходе между вкладками. Пробрасывать колбэк из ленты через
 * роутер нечем, поэтому экран кладёт действие в контекст, а шапка его берёт.
 *
 * В контексте лежит не готовый узел, а описание кнопки: узел пересоздавался бы
 * каждый рендер и гонял бы эффект по кругу.
 */
export const AppBarActionContext = createContext<AppBarAction | null>(null)

export const AppBarActionSetterContext = createContext<
  ((action: AppBarAction | null) => void) | null
>(null)

/** Действие текущего экрана. Читает шапка. */
export function useAppBarAction(): AppBarAction | null {
  return useContext(AppBarActionContext)
}

/**
 * Ставит действие шапки на время жизни экрана и снимает при уходе с него.
 *
 * `Icon` и `label` принимаются по отдельности, а не объектом: собрать объект
 * в вызывающем коде без `useMemo` нельзя, и забыть про это слишком легко.
 * `onClick` должен быть стабильным — оборачивайте в `useCallback`.
 */
export function useSetAppBarAction(Icon: LucideIcon, label: string, onClick: () => void): void {
  const setAction = useContext(AppBarActionSetterContext)
  const action = useMemo(() => ({ Icon, label, onClick }), [Icon, label, onClick])

  useEffect(() => {
    if (!setAction) return

    setAction(action)
    return () => setAction(null)
  }, [setAction, action])
}
