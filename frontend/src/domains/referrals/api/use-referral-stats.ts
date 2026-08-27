import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type ReferralStats } from '../types/referral'

import { referralKeys } from './referral-keys'

/** Сколько друзей пришло по ссылке и сколько за них начислено (S-47). */
export function useReferralStats(): UseQueryResult<ReferralStats, Error> {
  return useQuery({
    queryKey: referralKeys.stats(),
    queryFn: ({ signal }) => apiRequest<ReferralStats>('/api/referrals/stats', { signal }),
    staleTime: 60 * 1000,
  })
}
