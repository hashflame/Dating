import { useMutation, useQueryClient, type UseMutationResult } from '@tanstack/react-query'

import { feedKeys } from '@/domains/feed'
import { likeKeys } from '@/domains/likes'
import { matchKeys } from '@/domains/matches'
import { stub } from '@/shared/api'
import { ApiError } from '@/shared/api/api-error'

import { type ChatHandoff, type MessageKind } from '../types/message'

import { messageKeys } from './message-keys'
import { spendFixtureAllowance } from './use-message-limits'

export type OpenChatInput = {
  /** Кому пишем. Для суперсообщения мэтча ещё нет. */
  userId: string
  kind: MessageKind
  /**
   * Текст. У обычного сообщения — заготовка, которую человек унесёт в Telegram;
   * у суперсообщения — само сообщение: его прочтут в приложении.
   */
  text: string
  /** Есть только у сообщения мэтчу — по нему сервер находит пару. */
  matchId?: string
}

/**
 * Отправка сообщения (тикеты на бэкенд заведены).
 *
 * Сервер здесь делает три вещи разом: проверяет, что писать этому человеку
 * вообще можно, списывает зорки сверх недельного лимита и — для сообщения
 * мэтчу — отдаёт ссылку на личку. Одним запросом, а не тремя: платить и
 * получать ссылку надо атомарно, иначе списанные зорки могли бы остаться
 * без перехода.
 *
 * Суперсообщение ссылки не отдаёт (`chatUrl: null`): мэтча нет, и в Telegram
 * оно не ведёт. Текст сервер хранит у себя — получатель прочтёт его во
 * вкладке «Мэтчи».
 */
export function useOpenChat(): UseMutationResult<ChatHandoff, Error, OpenChatInput> {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input) => openChat(input),
    onSuccess: (result) => {
      queryClient.setQueryData(messageKeys.limits(), result.limits)
      void queryClient.invalidateQueries({ queryKey: ['viewer'] })
      void queryClient.invalidateQueries({ queryKey: likeKeys.root })
      void queryClient.invalidateQueries({ queryKey: matchKeys.root })
      // Суперсообщение — ещё и симпатия: этот человек больше не должен
      // всплыть в деке следующей карточкой.
      if (result.kind === 'super') {
        void queryClient.invalidateQueries({ queryKey: feedKeys.cards() })
      }
    },
  })
}

function openChat({ kind }: OpenChatInput): Promise<ChatHandoff> {
  const spent = spendFixtureAllowance(kind)

  if (spent === null) {
    return Promise.reject(
      new ApiError({
        status: 402,
        code: 'INSUFFICIENT_SPARKS',
        message: 'Не хватает зорок',
        action: 'TOP_UP_SPARKS',
      }),
    )
  }

  // @stub: `POST /api/messages` в backend/ нет — см. docs/api-gaps.md
  return stub('POST /api/messages', {
    messageId: crypto.randomUUID(),
    kind,
    sparksSpent: spent.sparksSpent,
    sparksBalance: spent.limits.sparksBalance,
    limits: spent.limits,
    // Суперсообщение остаётся в приложении — вести некуда.
    chatUrl: kind === 'super' ? null : 'https://t.me/durov',
    telegramUsername: 'durov',
  } satisfies ChatHandoff)
}
