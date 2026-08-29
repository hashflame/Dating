import { type MessageAllowance, type MessageKind, type MessageLimits } from '../types/message'

/** Во что обойдётся следующее сообщение этого вида. */
export type MessageCharge = {
  allowance: MessageAllowance
  /** Уложится в недельный лимит — платить не нужно. */
  free: boolean
  /** Сколько зорок спишется: 0, пока лимит не исчерпан. */
  cost: number
  /** Зорок не хватает — отправлять нечем, зовём пополнить баланс. */
  affordable: boolean
}

/**
 * Цена следующего сообщения. Одно место на все экраны: подпись на кнопке,
 * строка про остаток и решение, показывать ли шторку про лимит, должны
 * считаться одинаково.
 */
export function messageCharge(limits: MessageLimits, kind: MessageKind): MessageCharge {
  const allowance = kind === 'super' ? limits.superMessage : limits.message
  const free = allowance.remaining > 0

  return {
    allowance,
    free,
    cost: free ? 0 : allowance.cost,
    affordable: free || limits.sparksBalance >= allowance.cost,
  }
}
