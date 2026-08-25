/**
 * Сверено с backend: `Blizka.Api/Sparks/SparksDtos.cs`.
 *
 * Названия и порядок начислений сервер не присылает — только тип и сумму,
 * поэтому подписи живут в i18n на клиенте.
 */
export type SparkTransactionType =
  | 'registrationBonus'
  | 'profileCompletion'
  | 'verification'
  | 'referral'
  | 'ideaSubmission'
  | 'ideaImplemented'
  | 'contactUnlock'
  | 'superlike'
  | 'likesReveal'
  | 'purchase'

export type SparkTransaction = {
  id: string
  /** Со знаком: списание приходит отрицательным. */
  amount: number
  type: SparkTransactionType
  balanceAfter: number
  createdAt: string
}

export type SparksWallet = {
  balance: number
  history: {
    items: SparkTransaction[]
    page: number
    pageSize: number
    totalCount: number
    hasMore: boolean
  }
  /** Способы заработать: тип и сколько дают. */
  earnOptions: Array<{ type: SparkTransactionType; amount: number }>
}
