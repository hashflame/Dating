import { backButton } from '@tma.js/sdk-react'
import { useEffect } from 'react'

/**
 * Нативная кнопка «Назад» Telegram. `undefined` — не показывать.
 * Обработчик оборачивай в `useCallback`, иначе будет переподписываться.
 */
export function useBackButton(onClick: (() => void) | undefined): void {
  useEffect(() => {
    if (!onClick) return

    backButton.show()
    const off = backButton.onClick(onClick)

    return () => {
      off()
      backButton.hide()
    }
  }, [onClick])
}
