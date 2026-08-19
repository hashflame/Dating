/** Ошибка HTTP-запроса к API. Единственный тип ошибки, который бросает `apiRequest`. */
export class ApiError extends Error {
  readonly status: number
  readonly code: string | undefined
  readonly details: unknown

  constructor(params: { status: number; message: string; code?: string; details?: unknown }) {
    super(params.message)
    this.name = 'ApiError'
    this.status = params.status
    this.code = params.code
    this.details = params.details
  }

  get isUnauthorized(): boolean {
    return this.status === 401
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
