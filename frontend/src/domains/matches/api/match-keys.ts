/** Ключи кэша мэтчей. Хаб зависит от конкретного мэтча. */
export const matchKeys = {
  root: ['matches'] as const,
  list: () => [...matchKeys.root, 'list'] as const,
  hub: (matchId: string) => [...matchKeys.root, 'hub', matchId] as const,
}
