import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { stub } from '@/shared/api'

import { type MessageKind, type MessageLimits } from '../types/message'

import { messageKeys } from './message-keys'

const WEEK_MS = 7 * 24 * 60 * 60 * 1000

/**
 * Черновая касса лимитов на время заглушки: отправка из `use-send-message`
 * списывает отсюда, чтобы в dev-режиме были видны все состояния — есть
 * бесплатные, кончились, не хватает зорок. Уедет вместе с `stub()`.
 */
const FIXTURE: MessageLimits = {
  message: {
    used: 8,
    limit: 10,
    remaining: 2,
    resetsAt: new Date(Date.now() + WEEK_MS).toISOString(),
    cost: 3,
  },
  superMessage: {
    used: 0,
    limit: 1,
    remaining: 1,
    resetsAt: new Date(Date.now() + WEEK_MS).toISOString(),
    cost: 10,
  },
  sparksBalance: 52,
}

/**
 * Недельные лимиты сообщений и цена сверх них (тикет «обновить логику
 * сообщений»).
 *
 * Лимиты показываются в трёх местах — профиль, мэтчи, шторка отправки, —
 * поэтому живут отдельным запросом, а не полем каждого экрана.
 */
export function useMessageLimits(): UseQueryResult<MessageLimits, Error> {
  return useQuery({
    queryKey: messageKeys.limits(),
    // @stub: `GET /api/messages/limits` в backend/ нет — см. docs/api-gaps.md
    queryFn: () => stub('GET /api/messages/limits', structuredClone(FIXTURE)),
    staleTime: 60 * 1000,
  })
}

/**
 * Списание из черновой кассы заглушки. `null` — платить нечем: бесплатные
 * кончились, а зорок на платное не хватает; тогда ничего не списываем.
 */
export function spendFixtureAllowance(
  kind: MessageKind,
): { limits: MessageLimits; sparksSpent: number } | null {
  const key = kind === 'super' ? 'superMessage' : 'message'
  const allowance = FIXTURE[key]
  const free = allowance.remaining > 0

  if (!free && FIXTURE.sparksBalance < allowance.cost) return null

  const sparksSpent = free ? 0 : allowance.cost

  FIXTURE[key] = {
    ...allowance,
    used: allowance.used + 1,
    remaining: Math.max(0, allowance.remaining - 1),
  }
  FIXTURE.sparksBalance -= sparksSpent

  return { limits: structuredClone(FIXTURE), sparksSpent }
}
