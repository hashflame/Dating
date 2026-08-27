/** Ключи кэша рефералов. Ссылка у пользователя одна, статистику меняют друзья. */
export const referralKeys = {
  root: ['referrals'] as const,
  invite: () => [...referralKeys.root, 'invite'] as const,
  stats: () => [...referralKeys.root, 'stats'] as const,
}
