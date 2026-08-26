import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { apiRequest } from '@/shared/api'

import { type ProfilePatch, type Viewer } from '../types/viewer'

import { viewerKeys } from './viewer-keys'

/** Ответ `PATCH /api/users/me/profile`: профиль после патча и бонус за взятый порог. */
type ProfilePatchResult = {
  profile: Viewer
  /** Зорки за впервые достигнутый порог заполненности; 0 — порог не взят. */
  sparksAwarded: number
}

/**
 * Правка своей анкеты (T-9.1, S-40).
 *
 * Частичная: непереданное поле сервер оставляет как есть. Ответ уже содержит
 * профиль с пересчитанной заполненностью, поэтому кладём его в кэш сразу, а
 * инвалидируем только превью — оно собирается отдельным запросом.
 */
export function useUpdateProfile(): UseMutationResult<ProfilePatchResult, Error, ProfilePatch> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (patch) =>
      apiRequest<ProfilePatchResult>('/api/users/me/profile', { method: 'PATCH', body: patch }),
    onSuccess: (result) => {
      queryClient.setQueryData(viewerKeys.me(), result.profile)
      void queryClient.invalidateQueries({ queryKey: viewerKeys.preview() })
    },
  })
}
