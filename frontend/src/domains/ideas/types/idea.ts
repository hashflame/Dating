/**
 * Статус идеи на доске (S-60). Сверено с backend:
 * `Blizka.App/Domain/Enums/IdeaStatus.cs` — энумы сериализуются camelCase.
 */
export type IdeaStatus = 'new' | 'underReview' | 'planned' | 'implemented' | 'declined'

/** Статусы, которые на доске читаются как «этим занимаются». */
export const IN_PROGRESS_STATUSES: readonly IdeaStatus[] = ['underReview', 'planned']

/**
 * Идея с доски. Собрано по `Blizka.App/Domain/Entities/Idea.cs` и разделу
 * T-19.1 `decomposition.md` — самого эндпоинта ещё нет, см. docs/api-gaps.md.
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

/** Вкладки доски (S-60). Первые две — это `?sort=`, вторые две — фильтры. */
export type IdeaTab = 'hot' | 'new' | 'inProgress' | 'mine'
