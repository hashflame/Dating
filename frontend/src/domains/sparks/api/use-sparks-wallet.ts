import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type SparksWallet } from '../types/wallet'

/**
 * Кошелёк зорок (S-46): баланс, способы заработать и история операций.
 *
 * Берём первую страницу истории: `page`/`pageSize` сервер принимает, но
 * подгрузку следующих добавим, когда историй станет больше одного экрана —
 * пока в ответе `hasMore` и общее число, их и показываем.
 */
export function useSparksWallet(): UseQueryResult<SparksWallet, Error> {
  return useQuery({
    queryKey: ['sparks', 'wallet'],
    queryFn: ({ signal }) => apiRequest<SparksWallet>('/api/sparks/wallet', { signal }),
    staleTime: 60 * 1000,
  })
}
