/**
 * Сверено с backend: `Blizka.Api/Sparks/SparksDtos.cs`.
 *
 * Порядок начислений задаёт сервер, подписи он же и локализует (фикс T-8.1).
 */
export type SparkTransactionType =
  | 'registrationBonus'
  | 'profileCompletion'
  | 'verification'
  | 'referral'
  | 'ideaSubmission'
  | 'ideaImplemented'
  | 'contactUnlock'
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

/** Способ заработать зорки — строка списка «Как заработать» (S-46). */
export type SparkEarnOption = {
  type: SparkTransactionType
  /** Для `profileCompletion` — сумма за один порог (60/80/100%). */
  amount: number
  /** Локализованное сервером название. */
  label: string
  /** Текущий прогресс к `threshold`; `null` — прогресс к типу неприменим. */
  progress: number | null
  threshold: number | null
  /** Одноразовое уже получено либо выбраны все пороги. */
  completed: boolean
  /** Использовано в этом месяце; `null` — лимита нет или он не отслеживается. */
  usedThisMonth: number | null
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
  earnOptions: SparkEarnOption[]
}
