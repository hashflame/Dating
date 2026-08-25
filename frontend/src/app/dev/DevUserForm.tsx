import { useState } from 'react'

import { env } from '@/shared/config'
import { getDevUser, setDevUserId } from '@/shared/telegram'
import { Button, Input } from '@/shared/ui'

/**
 * Вход под другим Telegram-аккаунтом — то же, что логин в Telegram, только
 * вместо клиента подпись ставит dev-сервер. Нужен, чтобы проверять сценарии
 * с двумя людьми (мэтч, входящие симпатии) и заходить под демо-аккаунтами
 * `990000000001`–`990000000010`.
 *
 * Одно поле и есть весь логин: бэкенд опознаёт человека по id, имя и юзернейм
 * у существующего аккаунта берутся из базы. Своё имя с юзернеймом подставляются
 * только своему id — из `DEV_USER_*` в `.env`.
 *
 * Применяется полной перезагрузкой: initData собирается один раз при старте,
 * до первого запроса.
 *
 * Внутри настоящего Telegram скрыт: там initData приходит от клиента с подписью
 * реального пользователя, и подменить его нечем.
 */
export function DevUserForm() {
  const [id, setId] = useState(String(getDevUser().id))

  const parsedId = Number(id)
  const valid = Number.isSafeInteger(parsedId) && parsedId > 0

  if (!env.mockTelegram) return null

  const handleApply = (): void => {
    setDevUserId(parsedId)
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

      <Button size="sm" block disabled={!valid} onClick={handleApply}>
        Войти этим пользователем
      </Button>
    </div>
  )
}
