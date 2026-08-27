import { type IdeaTab } from '../types/idea'

/** Ключи кэша доски идей. Вкладка входит в ключ: у каждой свой список. */
export const ideaKeys = {
  root: ['ideas'] as const,
  list: (tab: IdeaTab) => [...ideaKeys.root, 'list', tab] as const,
}
