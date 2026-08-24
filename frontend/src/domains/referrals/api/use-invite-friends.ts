import { useMutation, type UseMutationResult } from '@tanstack/react-query'

import { stub } from '@/shared/api'

import { type ReferralInvite } from '../types/referral'

/**
 * Ссылка-приглашение для друзей.
 *
 * Реального эндпоинта нет, поэтому ссылка выдуманная — но путь до буфера
 * обмена настоящий, и заменить заглушку на `apiRequest` будет одной строкой.
 */
export function useInviteFriends(): UseMutationResult<ReferralInvite, Error, void> {
  return useMutation({
    // @stub: POST /api/referrals/invite на бэкенде нет — см. docs/api-gaps.md
    mutationFn: () =>
      stub('POST /api/referrals/invite', { link: 'https://t.me/blizka_bot?start=ref_dev' }),
  })
}
