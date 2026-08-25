/** Ключи кэша своего профиля. Инвалидируются после правок и начислений зорок. */
export const viewerKeys = {
  root: ['viewer'] as const,
  me: () => [...viewerKeys.root, 'me'] as const,
  preview: () => [...viewerKeys.root, 'preview'] as const,
}
