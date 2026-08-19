import { hapticFeedback } from '@tma.js/sdk-react'
import { useMemo } from 'react'

type Haptic = {
  /** Лёгкий отклик на нажатие. */
  tap: () => void
  /** Отклик на успешное действие. */
  success: () => void
  /** Отклик на ошибку. */
  error: () => void
  /** Отклик на выбор элемента из списка. */
  select: () => void
}

const noop = (): void => {}

/** Тактильная отдача Telegram. Безопасно вызывать в клиентах без поддержки. */
export function useHaptic(): Haptic {
  return useMemo<Haptic>(() => {
    const impact = hapticFeedback.impactOccurred.isAvailable()
      ? hapticFeedback.impactOccurred
      : undefined
    const notification = hapticFeedback.notificationOccurred.isAvailable()
      ? hapticFeedback.notificationOccurred
      : undefined
    const selection = hapticFeedback.selectionChanged.isAvailable()
      ? hapticFeedback.selectionChanged
      : undefined

    return {
      tap: impact ? () => impact('light') : noop,
      success: notification ? () => notification('success') : noop,
      error: notification ? () => notification('error') : noop,
      select: selection ? () => selection() : noop,
    }
  }, [])
}
