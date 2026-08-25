import { useState } from 'react'

import { useFeed, useUndoSwipe } from '@/domains/feed'
import { useDeleteAccount } from '@/domains/viewer'
import { isApiError } from '@/shared/api'
import { getDevUser } from '@/shared/telegram'
import { Button } from '@/shared/ui'

/**
 * | Кнопка          | Эндпоинт                       | Что делает                     |
 * | --------------- | ------------------------------ | ------------------------------ |
 * | Частичный сброс | `POST /api/feed/undo` в цикле  | возвращает свайпы в ленту      |
 * | Полный сброс    | `DELETE /api/users/me/account` | soft delete аккаунта (T-16.2)  |
 *
 * Частичный сброс не трогает анкету: онбординг остаётся пройденным, меняется
 * только лента. Стереть все свайпы одним запросом пока нечем — `undo` ограничен
 * тремя отменами в сутки, а `DELETE /api/onboarding/draft` заодно сбрасывает
 * регистрацию. Нужен отдельный эндпоинт, см. docs/api-gaps.md.
 *
 * «Полный сброс» необратим: восстановления в API нет, и повторный вход этим же
 * Telegram-id вернёт `410 USER_DELETED`. Поэтому он в два нажатия — случайный
 * клик не должен стоить тестового аккаунта.
 */
export function DevResetButtons() {
  const undo = useUndoSwipe()
  const feed = useFeed()
  const deleteAccount = useDeleteAccount()
  const [confirming, setConfirming] = useState(false)
  const [note, setNote] = useState<string | null>(null)

  const pending = undo.isPending || deleteAccount.isPending

  /** Возвращаем свайпы, пока сервер разрешает, и оставляем анкету пройденной. */
  const resetData = async (): Promise<void> => {
    setNote(null)
    setConfirming(false)

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

  const handleDelete = (): void => {
    if (!confirming) {
      setNote(null)
      setConfirming(true)
      return
    }

    deleteAccount.mutate(undefined, {
      onSuccess: () => window.location.reload(),
      onError: () => {
        setConfirming(false)
        setNote('Не удалось удалить аккаунт')
      },
    })
  }

  return (
    <div className="flex flex-col gap-2">
      <Button
        size="sm"
        variant="secondary"
        block
        disabled={pending}
        onClick={() => void resetData()}
      >
        Частичный сброс
      </Button>

      <Button
        size="sm"
        variant={confirming ? 'destructive' : 'secondary'}
        block
        disabled={pending}
        onClick={handleDelete}
      >
        {confirming ? `Удалить ${getDevUser().id}? Обратно нельзя` : 'Полный сброс'}
      </Button>

      {confirming && (
        <p className="text-tiny text-muted-foreground">
          Аккаунт удалится навсегда: вход этим Telegram-id будет отдавать 410, восстановления в API
          нет. Дальше тестировать можно, вписав другой Telegram-id выше.
        </p>
      )}

      {note && <p className="text-tiny text-muted-foreground">{note}</p>}
    </div>
  )
}

/** Почему сервер отказал вернуть свайп. */
function undoFailure(error: unknown): string {
  if (!isApiError(error)) return 'сервер отказал'
  if (error.code === 'UNDO_LIMIT_EXCEEDED') return 'лимит трёх отмен в сутки исчерпан'
  if (error.code === 'NOTHING_TO_UNDO') return 'возвращать нечего'

  return 'сервер отказал'
}
