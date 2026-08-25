import { useState } from 'react'

import { env } from '@/shared/config'
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
 *
 * Юзернейм бэкенд сохраняет в `TelegramUsername` при каждом входе, и из него
 * же собирается ссылка на аватар — то есть от него зависят и открытие контакта
 * в мэтче, и шаг «взять фото из Telegram».
 *
 * Внутри настоящего Telegram скрыт: там initData приходит от клиента с подписью
 * реального пользователя, и подменить его нечем.
 */
export function DevUserForm() {
  const current = getDevUser()
  const [id, setId] = useState(String(current.id))
  const [firstName, setFirstName] = useState(current.firstName)
  const [username, setUsername] = useState(current.username)

  const parsedId = Number(id)
  const valid = Number.isSafeInteger(parsedId) && parsedId > 0

  if (!env.mockTelegram) return null

  const handleApply = (): void => {
    setDevUser({ id: parsedId, firstName, username })
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

      <label className="text-tiny text-muted-foreground">
        Юзернейм
        <Input
          value={username}
          onChange={(event) => setUsername(event.target.value)}
          placeholder="без @"
          className="mt-1 h-9"
        />
      </label>

      <Button size="sm" block disabled={!valid} onClick={handleApply}>
        Войти этим пользователем
      </Button>
    </div>
  )
}
