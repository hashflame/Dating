/** Сверено с backend: `Blizka.Api/Likes/LikesDtos.cs`. */
export type IncomingLikes = {
  /** Сколько людей поставили лайк. Показывается бейджем на вкладке. */
  count: number
  /** Список раскрыт — за раскрытие списаны зорки. */
  revealed: boolean
  unlockCost: number
}
