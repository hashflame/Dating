export const photoKeys = {
  root: ['photos'] as const,
  list: () => [...photoKeys.root, 'list'] as const,
}
