/** ВАЖНО: контракт не сверён — `GET /api/users/me` на бэкенде нет. */
export type Viewer = {
  id: string
  telegramId: number
  firstName: string
  /** Баланс «зорок» — внутренней валюты. */
  balance: number
  isOnboarded: boolean
}
