import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { feedKeys } from '@/domains/feed'
import { likeKeys } from '@/domains/likes'
import { matchKeys } from '@/domains/matches'
import { apiRequest } from '@/shared/api'

import { type ReportReason } from '../types/moderation'

import { moderationKeys } from './moderation-keys'

type ReportInput = {
  userId: string
  reason: ReportReason
  /** Необязательный комментарий из формы жалобы (S-13). */
  comment?: string
  /** Галочка «также заблокировать» — сервер поставит блокировку сам. */
  blockUser: boolean
}

/**
 * Жалоба на пользователя (T-17.1, S-13). Критичные причины (`underage`,
 * `unsafeMeeting`) блокируют аккаунт немедленно, три жалобы за сутки — шэдоубан
 * до ручной проверки. При `blockUser: true` заодно ставится блокировка, поэтому
 * инвалидируем те же списки, что и `useBlockUser`.
 */
export function useReportUser(): UseMutationResult<void, Error, ReportInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ userId, reason, comment, blockUser }) =>
      apiRequest<void>(`/api/users/${userId}/report`, {
        method: 'POST',
        body: { reason, comment: comment === '' ? null : comment, blockUser },
      }),
    onSuccess: (_data, { blockUser }) => {
      if (!blockUser) return

      void queryClient.invalidateQueries({ queryKey: feedKeys.root })
      void queryClient.invalidateQueries({ queryKey: likeKeys.root })
      void queryClient.invalidateQueries({ queryKey: matchKeys.root })
      void queryClient.invalidateQueries({ queryKey: moderationKeys.root })
    },
  })
}
