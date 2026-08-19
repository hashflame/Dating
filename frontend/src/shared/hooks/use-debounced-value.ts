import { useEffect, useState } from 'react'

/**
 * Значение с задержкой. Нужен там, где на каждый символ нельзя дёргать сервер:
 * поиск города, поиск по интересам.
 *
 * ```ts
 * const [query, setQuery] = useState('')
 * const debouncedQuery = useDebouncedValue(query, 300)
 * const { data } = useCities(debouncedQuery)
 * ```
 */
export function useDebouncedValue<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value)

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs)

    return () => clearTimeout(timer)
  }, [value, delayMs])

  return debounced
}
