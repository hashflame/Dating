/** Ключи кэша приватности. Настройка одна на пользователя — ключ без параметров. */
export const privacyKeys = {
  root: ['privacy'] as const,
  settings: () => [...privacyKeys.root, 'settings'] as const,
}
