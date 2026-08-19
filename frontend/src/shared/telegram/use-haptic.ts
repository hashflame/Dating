import { hapticFeedback } from '@tma.js/sdk-react'
import { useMemo } from 'react'

type Haptic = {
  tap: () => void
  success: () => void
  error: () => void
  select: () => void
}

const noop = (): void => {}

/** Тактильная отдача. В клиентах без поддержки вызовы безопасны. */
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
