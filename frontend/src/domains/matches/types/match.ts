/** Сверено с backend: `Blizka.Api/Matches/MatchDtos.cs`. */
export type UnlockedContact = {
  /** `null`, если у человека нет username — тогда открываем по deepLink. */
  telegramUsername: string | null
  deepLink: string | null
  sparksSpent: number
  sparksBalance: number
}
