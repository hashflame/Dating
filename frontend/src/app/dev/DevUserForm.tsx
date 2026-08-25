import { useState } from 'react'

import { getDevUser, setDevUser } from '@/shared/telegram'
import { Button, Input } from '@/shared/ui'

/**
 * От чьего имени работаем в браузере.
 *
 * Подставь свой Telegram-id — и на стенде это будет твой настоящий аккаунт,
 * тот же, в который попадёшь из Telegram. Значение живёт в `localStorage`,
 * в репозиторий ничего не уезжает.
 *
 * Применяется полной перезагрузкой: initData собирается один раз при старте,
 * до первого запроса.
 */
export function DevUserForm() {
  const current = getDevUser()
  const [id, setId] = useState(String(current.id))
  const [firstName, setFirstName] = useState(current.firstName)

  const parsedId = Number(id)
  const valid = Number.isSafeInteger(parsedId) && parsedId > 0

  const handleApply = (): void => {
    setDevUser({ id: parsedId, firstName })
    window.location.reload()
  }

  return (
    <div className="flex flex-col gap-2">
      <label className="text-tiny text-muted-foreground">
        Telegram-id
        <Input
          value={id}
          onChange={(event) => setId(event.target.value.replace(/\D/g, ''))}
          inputMode="numeric"
          className="mt-1 h-9"
        />
      </label>

      <label className="text-tiny text-muted-foreground">
        Имя
        <Input
          value={firstName}
          onChange={(event) => setFirstName(event.target.value)}
          className="mt-1 h-9"
        />
      </label>

      <Button size="sm" block disabled={!valid} onClick={handleApply}>
        Войти этим пользователем
      </Button>
    </div>
  )
}
