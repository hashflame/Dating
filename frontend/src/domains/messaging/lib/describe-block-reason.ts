import { isApiError } from '@/shared/api'

import { type MessageBlockReason } from '../types/message'

/** Коды сервера → причины отказа. Держим списком: строки из ответа не гадаем. */
const CODES: Record<string, MessageBlockReason> = {
  RECIPIENT_BLOCKS_MESSAGES: 'blocksMessages',
  RECIPIENT_DELETED: 'deleted',
  RECIPIENT_BLOCKED: 'blocked',
  RECIPIENT_NO_USERNAME: 'noUsername',
  INSUFFICIENT_SPARKS: 'noSparks',
}

/**
 * Почему сервер не дал перейти в чат.
 *
 * Отдельная функция, а не `error.message` как есть: про удалённый аккаунт и
 * запрет на входящие человеку надо сказать по-разному — в первом случае писать
 * некому вообще, во втором собеседник напишет сам, — и оба раза без слова
 * «ошибка», потому что ничего не сломалось.
 */
export function describeBlockReason(error: unknown): MessageBlockReason {
  if (!isApiError(error)) return 'unknown'
  if (error.status === 402) return 'noSparks'

  return CODES[error.code] ?? 'unknown'
}
