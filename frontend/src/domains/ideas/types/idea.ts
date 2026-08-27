/**
 * Статус идеи на доске (S-60). Сверено с backend:
 * `Blizka.App/Domain/Enums/IdeaStatus.cs` — энумы сериализуются camelCase.
 */
export type IdeaStatus = 'new' | 'underReview' | 'planned' | 'implemented' | 'declined'

/** Статусы, которые на доске читаются как «этим занимаются». */
export const IN_PROGRESS_STATUSES: readonly IdeaStatus[] = ['underReview', 'planned']

/**
 * Идея с доски (T-19.1). Сверено с backend: `Blizka.Api/Ideas/IdeaDtos.cs` (`IdeaDto`).
 */
export type Idea = {
  id: string
  text: string
  status: IdeaStatus
  votesCount: number
  /** Голосовал ли текущий пользователь — от этого зависит вид кнопки. */
  hasVoted: boolean
  /** `null` — автор скрыл имя (`isAnonymous`). */
  authorName: string | null
  /** Идею предложил текущий пользователь — она попадает во вкладку «Мои». */
  isMine: boolean
  createdAt: string
}

/**
 * Вкладки доски (S-60) — все четыре идут в один query-параметр `?tab=`.
 * `inProgress` на сервере называется `inWork` (см. `use-ideas.ts`).
 */
export type IdeaTab = 'hot' | 'new' | 'inProgress' | 'mine'
