import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'

import { useDeleteAccount } from '@/domains/viewer'
import { apiRequest } from '@/shared/api'
import { getDevUser } from '@/shared/telegram'
import { Button, Input } from '@/shared/ui'

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
 * Telegram-id вернёт `410 USER_DELETED` навсегда. Двух нажатий подряд оказалось
 * мало — так уже потеряли один тестовый аккаунт, — поэтому кнопка спрятана под
 * «Опасная зона» и требует вписать Telegram-id руками. Промахнуться пальцем или
 * автотестом по ней больше нельзя.
 */
export function DevResetButtons() {
  const queryClient = useQueryClient()
  const deleteAccount = useDeleteAccount()
  const [resetting, setResetting] = useState(false)
  const [dangerOpen, setDangerOpen] = useState(false)
  const [confirmId, setConfirmId] = useState('')
  const [note, setNote] = useState<string | null>(null)

  const pending = resetting || deleteAccount.isPending

  const resetState = async (): Promise<void> => {
    setNote(null)
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

  const devUserId = String(getDevUser().id)
  const confirmed = confirmId.trim() === devUserId

  const handleDelete = (): void => {
    if (!confirmed) return

    deleteAccount.mutate(undefined, {
      onSuccess: () => window.location.reload(),
      onError: () => setNote('Не удалось удалить аккаунт'),
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

      {dangerOpen ? (
        <div className="flex flex-col gap-2 rounded-md border border-destructive/40 p-2">
          <p className="text-tiny text-muted-foreground">
            Аккаунт удалится навсегда: вход этим Telegram-id будет отдавать 410, восстановления в
            API нет. Впишите {devUserId}, чтобы подтвердить.
          </p>

          <Input
            value={confirmId}
            onChange={(event) => setConfirmId(event.target.value.replace(/D/g, ''))}
            inputMode="numeric"
            placeholder={devUserId}
            className="h-9"
          />

          <Button
            size="sm"
            variant="destructive"
            block
            disabled={!confirmed || pending}
            onClick={handleDelete}
          >
            Удалить аккаунт навсегда
          </Button>

          <Button size="sm" variant="ghost" block onClick={() => setDangerOpen(false)}>
            Отмена
          </Button>
        </div>
      ) : (
        <Button size="sm" variant="ghost" block onClick={() => setDangerOpen(true)}>
          Опасная зона
        </Button>
      )}

      {note && <p className="text-tiny text-muted-foreground">{note}</p>}
    </div>
  )
}
