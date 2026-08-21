import { useEffect, useState } from 'react'

/** Значение с задержкой: не дёргаем сервер на каждый символ в поиске. */
export function useDebouncedValue<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value)

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs)

    return () => clearTimeout(timer)
  }, [value, delayMs])

  return debounced
}
