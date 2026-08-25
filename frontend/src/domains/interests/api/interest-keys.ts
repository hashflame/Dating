/** Ключи кэша интересов. Локаль в ключе: названия приходят переведёнными. */
export const interestKeys = {
  root: ['interests'] as const,
  catalog: (locale: string) => [...interestKeys.root, 'catalog', locale] as const,
  search: (query: string, locale: string) =>
    [...interestKeys.root, 'search', locale, query] as const,
}
