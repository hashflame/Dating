/** Ключи кэша блокировок. Список заблокированных живёт в настройках приватности. */
export const moderationKeys = {
  root: ['moderation'] as const,
  blocked: () => [...moderationKeys.root, 'blocked'] as const,
}
