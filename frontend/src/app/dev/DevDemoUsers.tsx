import { useState } from 'react'

import { apiRequest, isApiError } from '@/shared/api'
import { env } from '@/shared/config'
import { setDevUser } from '@/shared/telegram'
import { Button, ListRow } from '@/shared/ui'

/** Сверено с backend: `src/Blizka.Api/Dev/DevDtos.cs` (спека 003). */
type DemoAccount = {
  telegramId: number
  username: string
  name: string
  mainPhotoUrl: string | null
}

type ReseedDemoDataResponse = {
  users: DemoAccount[]
}

function reseedDemoData(): Promise<ReseedDemoDataResponse> {
  return apiRequest<ReseedDemoDataResponse>('/api/dev/reseed-demo-data', {
    method: 'POST',
    auth: 'none',
  })
}

function reseedFailure(error: unknown): string {
  if (isApiError(error) && error.isUnauthorized) {
    return 'DEV_LOGIN_SECRET не задан или не совпадает с бэкендом — см. docs/real-backend.md'
  }

  return 'сервер отказал'
}

/**
 * ВРЕМЕННЫЙ инструмент только для ручного тестирования (спека 003 в backend,
 * `docs/specs/003-demo-seed-data.md`) — вход под одним из 10 фиксированных
 * демо-аккаунтов с готовыми анкетами/фото/мэтчами, без прохождения онбординга.
 * Убрать вместе с `vite/dev-login-auth.ts`, когда демо-режим на бэкенде выключат.
 *
 * Секрет для запроса подставляет dev-сервер (`vite/dev-login-auth.ts`), сюда он
 * не попадает — компонент лишь дёргает эндпоинт и показывает результат.
 */
export function DevDemoUsers() {
  const [users, setUsers] = useState<DemoAccount[] | null>(null)
  const [pending, setPending] = useState(false)
  const [note, setNote] = useState<string | null>(null)

  if (!env.mockTelegram) return null

  const handleReseed = (): void => {
    setPending(true)
    setNote(null)
    reseedDemoData()
      .then((response) => setUsers(response.users))
      .catch((error: unknown) => setNote(`Не пересидировал: ${reseedFailure(error)}`))
      .finally(() => setPending(false))
  }

  const handleLoginAs = (user: DemoAccount): void => {
    setDevUser({ id: user.telegramId, firstName: user.name, username: user.username })
    window.location.reload()
  }

  return (
    <div className="flex flex-col gap-2">
      <Button size="sm" variant="secondary" block disabled={pending} onClick={handleReseed}>
        Пересидировать демо-данные
      </Button>

      {note && <p className="text-tiny text-muted-foreground">{note}</p>}

      {users && (
        <div className="overflow-hidden rounded-2xl border border-border">
          {users.map((user) => (
            <ListRow
              key={user.telegramId}
              title={user.name}
              subtitle={`@${user.username}`}
              onClick={() => handleLoginAs(user)}
              leading={
                user.mainPhotoUrl ? (
                  <img
                    src={user.mainPhotoUrl}
                    alt=""
                    className="size-9 shrink-0 rounded-full object-cover"
                  />
                ) : (
                  <div className="size-9 shrink-0 rounded-full bg-accent" />
                )
              }
            />
          ))}
        </div>
      )}
    </div>
  )
}
