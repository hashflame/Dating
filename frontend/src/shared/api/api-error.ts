/**
 * Единственный тип ошибки, который бросает `apiRequest`.
 * Бэкенд отвечает `{ error: { code, message, details, action } }`, где `message`
 * уже локализован сервером — его можно показывать пользователю как есть.
 */
export class ApiError extends Error {
  readonly status: number
  /** `VALIDATION_ERROR`, `INSUFFICIENT_SPARKS`, … */
  readonly code: string
  readonly details: unknown
  /** Подсказка для кнопки: `TOP_UP_SPARKS`, `COMPLETE_ONBOARDING`, … */
  readonly action: string | null

  constructor(params: {
    status: number
    code: string
    message: string
    details?: unknown
    action?: string | null
  }) {
    super(params.message)
    this.name = 'ApiError'
    this.status = params.status
    this.code = params.code
    this.details = params.details
    this.action = params.action ?? null
  }

  get isUnauthorized(): boolean {
    return this.status === 401
  }

  get isForbidden(): boolean {
    return this.status === 403
  }

  get isNotFound(): boolean {
    return this.status === 404
  }

  get isServer(): boolean {
    return this.status >= 500
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError
}
