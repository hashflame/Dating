import { type UserStatus } from '@/domains/session'

/** Сверено с backend: `Blizka.Api/Users/UserMeResponse.cs`. */
export type Viewer = {
  id: string
  telegramId: number
  name: string
  /** Баланс «зорок» — внутренней валюты. */
  sparksBalance: number
  status: UserStatus
  locale: string
}
