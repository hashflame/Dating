/** Ключи кэша мэтчей. Хаб и вопрос дня зависят от конкретного мэтча. */
export const matchKeys = {
  root: ['matches'] as const,
  list: () => [...matchKeys.root, 'list'] as const,
  hub: (matchId: string) => [...matchKeys.root, 'hub', matchId] as const,
  question: (matchId: string) => [...matchKeys.root, 'question', matchId] as const,
  questionsArchive: (matchId: string) => [...matchKeys.root, 'questions', matchId] as const,
}
