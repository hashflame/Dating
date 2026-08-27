import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type Idea, type IdeaTab } from '../types/idea'

import { ideaKeys } from './idea-keys'

/** `inProgress` на фронте — это `inWork` в query-параметре сервера (T-19.1). */
const TAB_TO_QUERY: Record<IdeaTab, string> = {
  hot: 'hot',
  new: 'new',
  inProgress: 'inWork',
  mine: 'mine',
}

/** Хватает с запасом на одну доску без пагинации — сервер допускает максимум 50 (T-19.1). */
const PAGE_SIZE = 50

type IdeasPageResponse = {
  items: Idea[]
  page: number
  pageSize: number
  totalCount: number
}

/**
 * Доска идей (S-60). Берём первую страницу — подгрузку следующих добавим,
 * когда список станет больше одного экрана (по образцу `useSparksWallet`).
 */
export function useIdeas(tab: IdeaTab): UseQueryResult<Idea[], Error> {
  return useQuery({
    queryKey: ideaKeys.list(tab),
    queryFn: async ({ signal }) => {
      const page = await apiRequest<IdeasPageResponse>('/api/ideas', {
        query: { tab: TAB_TO_QUERY[tab], pageSize: PAGE_SIZE },
        signal,
      })
      return page.items
    },
  })
}
