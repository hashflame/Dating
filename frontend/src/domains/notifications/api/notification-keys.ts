/** Ключи кэша уведомлений. Счётчик один на пользователя — без параметров. */
export const notificationKeys = {
  root: ['notifications'] as const,
  unread: () => [...notificationKeys.root, 'unread'] as const,
}
