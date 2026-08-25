/** Сверено с backend: `Blizka.Api/Likes/LikesDtos.cs`. */
export type LikeUser = {
  userId: string
  name: string
  age: number
  /** `null` — фото нет, показываем градиентную подложку. */
  mainPhotoUrl: string | null
}

export type IncomingLikes = {
  /** Сколько людей поставили лайк. Показывается бейджем на вкладке. */
  count: number
  /** Список раскрыт — за раскрытие списаны зорки. */
  revealed: boolean
  unlockCost: number
  /**
   * Заблюренные превью главных фото. Приходят только пока `revealed: false`;
   * `blurredPhotoUrl` — data URI, сервер собирает его на лету.
   */
  preview: Array<{ blurredPhotoUrl: string }> | null
  /** Полный список — только когда `revealed: true`. */
  users: LikeUser[] | null
}

export type OutgoingLikes = {
  count: number
  users: LikeUser[]
}

export type RevealedLikes = {
  /** 0 при повторном вызове: раскрытие уже оплачено. */
  sparksSpent: number
  sparksBalance: number
  users: LikeUser[]
}
