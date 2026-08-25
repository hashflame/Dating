import { useState } from 'react'

import { useResetOnboarding } from '@/domains/onboarding'
import { useDeleteAccount } from '@/domains/viewer'
import { Button } from '@/shared/ui'

/**
 * Два сброса текущего аккаунта — чтобы переигрывать сценарии на одном
 * пользователе, а не заводить нового на каждый прогон.
 *
 * | Кнопка          | Эндпоинт                       | Что делает                            |
 * | --------------- | ------------------------------ | ------------------------------------- |
 * | Частичный сброс | `DELETE /api/onboarding/draft` | черновик, статус в `new`, свои свайпы |
 * | Полный сброс    | `DELETE /api/users/me/account` | soft delete аккаунта (T-16.2)         |
 *
 * «Полный сброс» необратим: восстановления в API нет, и повторный вход этим же
 * Telegram-id вернёт `410 USER_DELETED`. Поэтому он в два нажатия — случайный
 * клик не должен стоить тестового аккаунта.
 */
export function DevResetButtons() {
  const reset = useResetOnboarding()
  const deleteAccount = useDeleteAccount()
  const [confirming, setConfirming] = useState(false)
  const [note, setNote] = useState<string | null>(null)

  const pending = reset.isPending || deleteAccount.isPending

  const resetData = (): void => {
    setNote(null)
    setConfirming(false)
    reset.mutate(undefined, {
      onSuccess: () => window.location.reload(),
      onError: () => setNote('Не удалось сбросить'),
    })
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
      <Button size="sm" variant="secondary" block disabled={pending} onClick={resetData}>
        Частичный сброс
      </Button>

      <Button
        size="sm"
        variant={confirming ? 'destructive' : 'secondary'}
        block
        disabled={pending}
        onClick={handleDelete}
      >
        {confirming ? 'Точно удалить? Обратно нельзя' : 'Полный сброс'}
      </Button>

      {confirming && (
        <p className="text-tiny text-muted-foreground">
          Аккаунт удалится навсегда. Дальше тестировать можно, вписав другой Telegram-id выше.
        </p>
      )}

      {note && <p className="text-tiny text-muted-foreground">{note}</p>}
    </div>
  )
}
