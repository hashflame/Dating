import { Undo2 } from 'lucide-react'
import { useState } from 'react'

import { useUndoSwipe } from '@/domains/feed'

/**
 * Возвращает свайпы, чтобы переиграть карточки, не заводя нового пользователя.
 *
 * Полного сброса у API нет (см. docs/api-gaps.md), а отмена работает по одному
 * свайпу, поэтому зовём её подряд, пока сервер разрешает: одно нажатие
 * возвращает всё, что можно. Экран при этом не меняется — остаёшься в ленте.
 */
export function DevUndoSwipe() {
  const undo = useUndoSwipe()
  const [busy, setBusy] = useState(false)

  const handleClick = async (): Promise<void> => {
    setBusy(true)
    try {
      // Ограничение сервера неизвестно заранее, поэтому просто идём до отказа.
      for (;;) {
        await undo.mutateAsync()
      }
    } catch {
      // Отказ — значит вернули всё, что было можно.
    } finally {
      setBusy(false)
    }
  }

  return (
    <button
      type="button"
      onClick={() => void handleClick()}
      disabled={busy}
      aria-label="Вернуть свайпы: показать карточки заново (только для разработки)"
      title="Вернуть свайпы — карточки появятся снова"
      className="flex size-9 items-center justify-center rounded-full border border-border bg-card/90 text-muted-foreground shadow-sm backdrop-blur transition-colors hover:bg-accent disabled:opacity-50"
    >
      <Undo2 className="size-4" aria-hidden />
    </button>
  )
}
