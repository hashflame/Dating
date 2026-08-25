import { useState } from 'react'

import { useFeed, useUndoSwipe } from '@/domains/feed'
import { useResetOnboarding } from '@/domains/onboarding'
import { isApiError } from '@/shared/api'
import { Button } from '@/shared/ui'

/** Почему сервер отказал вернуть свайп. */
function undoFailure(error: unknown): string {
  if (!isApiError(error)) return 'сервер отказал'
  if (error.code === 'UNDO_LIMIT_EXCEEDED') return 'лимит отмен на сутки исчерпан'
  if (error.code === 'NOTHING_TO_UNDO') return 'возвращать нечего'

  return 'сервер отказал'
}

/**
 * Два сброса текущего аккаунта — чтобы переигрывать сценарии на одном
 * пользователе, а не заводить нового на каждый прогон.
 *
 * Оба опираются на `DELETE /api/onboarding/draft`: он чистит черновик и
 * возвращает статус в `new`. Зорки и фото при этом остаются — это не удаление
 * аккаунта, так задумано на бэкенде.
 */
export function DevResetButtons() {
  const reset = useResetOnboarding()
  const undo = useUndoSwipe()
  const feed = useFeed()
  const [note, setNote] = useState<string | null>(null)

  const toOnboarding = (): void => {
    setNote(null)
    reset.mutate(undefined, {
      onSuccess: () => window.location.reload(),
      onError: () => setNote('Не удалось сбросить'),
    })
  }

  /**
   * Возвращаем свайпы, насколько разрешает сервер, и оставляем анкету
   * пройденной. Полной очистки свайпов у API нет — см. docs/api-gaps.md,
   * поэтому дальше третьего подряд отмена упрётся в лимит.
   */
  const toCleanFeed = async (): Promise<void> => {
    setNote(null)
    let restored = 0
    try {
      for (;;) {
        await undo.mutateAsync()
        restored += 1
      }
    } catch (error) {
      setNote(restored > 0 ? `Вернул свайпов: ${restored}` : `Не вернул: ${undoFailure(error)}`)
    }
    await feed.refetch()
  }

  return (
    <div className="flex flex-col gap-2">
      <Button size="sm" variant="secondary" block disabled={reset.isPending} onClick={toOnboarding}>
        Сбросить до регистрации
      </Button>

      <Button
        size="sm"
        variant="secondary"
        block
        disabled={undo.isPending}
        onClick={() => void toCleanFeed()}
      >
        Вернуть свайпы
      </Button>

      {note && <p className="text-tiny text-muted-foreground">{note}</p>}
    </div>
  )
}
