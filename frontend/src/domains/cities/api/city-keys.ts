export const cityKeys = {
  root: ['cities'] as const,
  search: (query: string, locale: string) => [...cityKeys.root, 'search', query, locale] as const,
  byId: (cityId: string, locale: string) => [...cityKeys.root, 'byId', cityId, locale] as const,
}
