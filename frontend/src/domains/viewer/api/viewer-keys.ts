/**
 * Ключи кэша react-query для домена. Один домен — один объект ключей.
 * Инвалидация: `queryClient.invalidateQueries({ queryKey: viewerKeys.root })`.
 */
export const viewerKeys = {
  root: ['viewer'] as const,
  me: () => [...viewerKeys.root, 'me'] as const,
}
