import { stub } from '@/shared/api'

import { type Viewer } from '../types/viewer'

const VIEWER_FIXTURE: Viewer = {
  id: '00000000-0000-0000-0000-000000000001',
  telegramId: 99281932,
  firstName: 'Дзмітры',
  balance: 25,
  isOnboarded: false,
}

/**
 * Профиль текущего пользователя.
 *
 * Пример реального запроса, когда эндпоинт будет сверён с бэкендом:
 * `return apiRequest<Viewer>('/users/me', { signal })`
 */
export function getViewer(): Promise<Viewer> {
  // @stub: эндпоинт не сверен с backend/ — см. docs/api-gaps.md
  return stub('GET /users/me', VIEWER_FIXTURE)
}
