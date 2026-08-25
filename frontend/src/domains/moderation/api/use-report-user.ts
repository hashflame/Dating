import { useMutation, type UseMutationResult } from '@tanstack/react-query'

import { stub } from '@/shared/api'

import { type ReportReason } from '../types/moderation'

type ReportInput = {
  userId: string
  reason: ReportReason
}

/**
 * Жалоба на пользователя (S-13, спека §21.1). Блокировку отправляем отдельно:
 * в спеке это флаг `blockUser`, но по интерфейсу это два разных решения.
 */
export function useReportUser(): UseMutationResult<void, Error, ReportInput> {
  return useMutation({
    // @stub: POST /api/users/{userId}/report на бэкенде нет — см. docs/api-gaps.md
    mutationFn: ({ userId, reason }) =>
      stub<void>(`POST /api/users/${userId}/report (${reason})`, undefined),
  })
}
