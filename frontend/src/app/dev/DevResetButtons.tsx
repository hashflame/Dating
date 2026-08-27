import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'

import { useDeleteAccount } from '@/domains/viewer'
import { apiRequest } from '@/shared/api'
import { getDevUser } from '@/shared/telegram'
import { Button } from '@/shared/ui'

/**
 * | Кнопка          | Эндпоинт                        | Что делает                          |
 * | --------------- | ------------------------------- | ----------------------------------- |
 * | Частичный сброс | `POST /api/dev/reset-my-state`  | состояние «сразу после онбординга»   |
 * | Полный сброс    | `DELETE /api/users/me/account`  | soft delete аккаунта (T-16.2)        |
 *
 * Частичный сброс не трогает анкету: онбординг остаётся пройденным. Сервер
 * чистит свайпы обеих сторон, мэтчи, фото, интересы и предпочтения, обнуляет
 * пороговые бонусы и возвращает баланс к послерегистрационному. Прежний хак с
 * `POST /api/feed/undo` в цикле больше не нужен — он отменял только последние
 * свайпы и упирался в лимит трёх отмен в сутки.
 *
 * Запрос идёт мимо доменного слоя: эндпоинт дев-только, а `app/dev` вырезается
 * из production-сборки — хук в `domains/` тащил бы его в бандл.
 *
 * «Полный сброс» необратим: восстановления в API нет, и повторный вход этим же
 * Telegram-id вернёт `410 USER_DELETED`. Поэтому он в два нажатия — случайный
 * клик не должен стоить тестового аккаунта.
 */
export function DevResetButtons() {
  const queryClient = useQueryClient()
  const deleteAccount = useDeleteAccount()
  const [resetting, setResetting] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [note, setNote] = useState<string | null>(null)

  const pending = resetting || deleteAccount.isPending

  const resetState = async (): Promise<void> => {
    setNote(null)
    setConfirming(false)
    setResetting(true)

    try {
      await apiRequest<void>('/api/dev/reset-my-state', { method: 'POST' })
      // Сбросилось всё сразу — точечная инвалидация тут только запутает.
      await queryClient.invalidateQueries()
      setNote('Состояние сброшено до «сразу после онбординга»')
    } catch {
      setNote('Сервер отказал в сбросе')
    } finally {
      setResetting(false)
    }
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
        onClick={() => void resetState()}
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
