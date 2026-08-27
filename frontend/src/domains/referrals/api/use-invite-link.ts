import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type ReferralInvite } from '../types/referral'

import { referralKeys } from './referral-keys'

/**
 * Ссылка-приглашение (T-20.1, S-47).
 *
 * Запрос, хотя метод `POST`: код сервер выводит детерминированно из `userId` —
 * повторный вызов ничего не создаёт и всегда отдаёт одно и то же. Экранам
 * ссылка нужна сразу при отрисовке, а не по нажатию, поэтому это `useQuery`.
 */
export function useInviteLink(): UseQueryResult<ReferralInvite, Error> {
  return useQuery({
    queryKey: referralKeys.invite(),
    queryFn: ({ signal }) =>
      apiRequest<ReferralInvite>('/api/referrals/invite', { method: 'POST', signal }),
    staleTime: Infinity,
  })
}
