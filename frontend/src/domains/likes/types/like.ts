import { type SuperMessagePreview } from '@/domains/messaging'

/** Сверено с backend: `Blizka.Api/Likes/LikesDtos.cs`. */
export type LikeUser = {
  userId: string
  name: string
  /** `null` — человек включил «Скрывать возраст» (T-16.1). */
  age: number | null
  /** `null` — фото нет, показываем градиентную подложку. */
  mainPhotoUrl: string | null
  /**
   * Уже мэтч — раньше такие молча пропадали из списка (тикет ClickUp), теперь
   * остаются и помечаются: список оплачен, укорачивать его без предупреждения нельзя.
   */
  isMatched: boolean
  /** Id мэтча, если `isMatched` — чтобы открыть хаб мэтча, а не карточку профиля. */
  matchId: string | null
  /**
   * Человек написал суперсообщение, а не просто лайкнул. Заполнено только у
   * тех, кто пришёл в `IncomingLikes.superMessages`.
   *
   * Поле необязательное: бэкенд его пока не отдаёт (тикет «Суперсообщения:
   * отправка и выдача в симпатиях»), см. docs/api-gaps.md.
   */
  superMessage?: SuperMessagePreview | null
  /**
   * Мы отправили этому человеку суперсообщение — в «Ваших лайках» плитка
   * помечается, чтобы не написать второй раз.
   *
   * Поле необязательное по той же причине, что и `superMessage`: бэкенд
   * суперсообщения пока не хранит, см. docs/api-gaps.md. Пока его нет — метки
   * просто не будет.
   */
  superMessageSent?: boolean
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
  /**
   * Кто написал суперсообщение, а не просто лайкнул. Приходят независимо от
   * `revealed`: отправитель заплатил за то, чтобы его текст прочли, и держать
   * такой лайк за платным раскрытием списка нечестно. В `users` этих людей
   * нет — иначе один и тот же человек показался бы дважды.
   *
   * Показываем их во вкладке «Мэтчи», а не здесь: суперсообщение — это уже
   * начатый разговор, а не строка платного списка.
   *
   * Поле необязательное: бэкенд его пока не отдаёт, см. docs/api-gaps.md.
   * Пока его нет — секции в мэтчах просто не будет.
   */
  superMessages?: LikeUser[] | null
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
