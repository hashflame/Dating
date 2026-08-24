export const feedKeys = {
  root: ['feed'] as const,
  cards: () => [...feedKeys.root, 'cards'] as const,
  filters: () => [...feedKeys.root, 'filters'] as const,
}
