/** Ключи кэша симпатий. Отдельный файл: списками пользуются и экран, и нижнее меню. */
export const likeKeys = {
  root: ['likes'] as const,
  incoming: () => [...likeKeys.root, 'incoming'] as const,
  outgoing: () => [...likeKeys.root, 'outgoing'] as const,
}
