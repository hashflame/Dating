/** Ключи кэша сообщений: лимиты нужны и экранам, и шторкам отправки. */
export const messageKeys = {
  root: ['messages'] as const,
  limits: () => [...messageKeys.root, 'limits'] as const,
}
