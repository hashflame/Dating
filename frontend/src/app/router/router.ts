import { createMemoryHistory, createRouter } from '@tanstack/react-router'

import { ROUTES } from '@/shared/config'

import { routeTree } from './route-tree'

/**
 * Роутер на memory-history.
 *
 * Почему не браузерная история: Telegram передаёт launch params в `location.hash`
 * и управляет навигацией через нативную кнопку «Назад», а не через историю браузера.
 * Memory-history убирает конфликт и делает поведение одинаковым во всех клиентах.
 */
export const router = createRouter({
  routeTree,
  history: createMemoryHistory({ initialEntries: [ROUTES.home] }),
  defaultPreload: 'intent',
})

declare module '@tanstack/react-router' {
  // eslint-disable-next-line @typescript-eslint/consistent-type-definitions -- расширение типов библиотеки требует interface
  interface Register {
    router: typeof router
  }
}
