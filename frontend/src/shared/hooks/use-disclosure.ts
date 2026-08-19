import { useCallback, useMemo, useState } from 'react'

type Disclosure = {
  isOpen: boolean
  open: () => void
  close: () => void
  toggle: () => void
}

/** Открытие и закрытие модалки, шита, аккордеона. */
export function useDisclosure(initialOpen = false): Disclosure {
  const [isOpen, setIsOpen] = useState(initialOpen)

  const open = useCallback(() => setIsOpen(true), [])
  const close = useCallback(() => setIsOpen(false), [])
  const toggle = useCallback(() => setIsOpen((prev) => !prev), [])

  return useMemo(() => ({ isOpen, open, close, toggle }), [isOpen, open, close, toggle])
}
