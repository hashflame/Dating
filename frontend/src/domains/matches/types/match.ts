/** Сверено с backend: `Blizka.Api/Matches/MatchDtos.cs`. */
export type MatchUser = {
  userId: string
  name: string
  /** `null` — человек включил «Скрывать возраст» (T-16.1). */
  age: number | null
  mainPhotoUrl: string | null
}

/**
 * Три секции списка (S-30). Мэтч уходит в архив через 7 дней без переписки,
 * вернуть его можно бесплатно и всегда.
 */
export type Matches = {
  new: Array<{
    matchId: string
    user: MatchUser
    matchedAt: string
    /** Сколько зорок стоит открыть контакт. */
    contactCost: number
    /** У человека включено «Запретить писать мне» — он пишет первым сам. */
    writesFirst: boolean
    badge: string | null
  }>
  waitingForMessage: Array<{
    matchId: string
    user: MatchUser
    contactOpenedAt: string
    badge: string
  }>
  archived: Array<{
    matchId: string
    user: MatchUser
    archivedAt: string
    reason: string
  }>
}

/**
 * Открыт ли контакт. `locked` — нужно платить, `unlocked` — username доступен,
 * `writes_first_only` — человек включил «Запретить писать мне» (T-16.1, S-51):
 * платить не за что, первым он напишет сам.
 */
type ContactStatus = 'locked' | 'unlocked' | 'writes_first_only'

/**
 * Хаб мэтча (S-31) — центральный экран, из которого открываются ветки.
 * `telegramUsername` приходит, когда контакт открыт.
 */
export type MatchHub = {
  matchId: string
  user: {
    userId: string
    name: string
    /** `null` — человек включил «Скрывать возраст» (T-16.1). */
    age: number | null
    city: string
    telegramUsername: string | null
    mainPhotoUrl: string | null
  }
  compatibility: { score: number; details: string }
  contactStatus: ContactStatus
  /**
   * Что ещё доступно в паре. `questionOfDay` и `dateIdea` сервер всё ещё
   * отдаёт, но вопрос дня из приложения убран совсем, а идея свидания
   * переехала на экран «в разработке» — флаги для них больше не нужны.
   */
  features: {
    minigame: { available: boolean }
    staleConversation: { available: boolean }
  }
}
