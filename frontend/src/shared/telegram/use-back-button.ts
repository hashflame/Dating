import { backButton } from '@tma.js/sdk-react'
import { useEffect } from 'react'

/**
 * Показывает нативную кнопку «Назад» Telegram и вешает на неё обработчик.
 * Кнопка скрывается при размонтировании.
 *
 * @param onClick - что делать по нажатию; `undefined` — кнопку не показывать.
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
