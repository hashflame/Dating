import { useCallback, useEffect, useRef } from 'react'

/**
 * Сколько секунд прошло с момента, как экран (или карточка) появился.
 *
 * Возвращает функцию, а не число: время нужно один раз, в момент отправки
 * события, и тикающее состояние заставляло бы перерисовывать экран каждую
 * секунду впустую.
 *
 * `resetKey` перезапускает отсчёт, не перемонтируя компонент, — так лента
 * считает время на каждой карточке отдельно.
 */
export function useElapsedSeconds(resetKey?: unknown): () => number {
  // Отсчёт начинает эффект, а не рендер: `Date.now()` в теле хука — вызов
  // нечистой функции, и рендер от него становится непредсказуемым.
  const startedAt = useRef(0)

  useEffect(() => {
    startedAt.current = Date.now()
  }, [resetKey])

  return useCallback(
    () => (startedAt.current === 0 ? 0 : Math.round((Date.now() - startedAt.current) / 1000)),
    [],
  )
}
