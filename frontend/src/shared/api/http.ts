import { env } from '@/shared/config'
import { getDevUser } from '@/shared/telegram'
import { getRawInitData } from '@/shared/telegram/bridge'

import { ApiError } from './api-error'
import { getAuthToken } from './auth-token'

type QueryValue = string | number | boolean | undefined | null

/**
 * `telegram` — initData в заголовке, его принимает только `POST /api/auth/telegram`.
 * `dev-login` — ВРЕМЕННО (спека 003 в backend): секрет демо-входа для
 * `POST /api/dev/reseed-demo-data`, единственного эндпоинта, где нужен именно
 * секрет без Telegram-id.
 */
type AuthMode = 'bearer' | 'telegram' | 'dev-login' | 'none'

type ApiRequestOptions = {
  method?: 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE'
  /** JSON-тело запроса. Для загрузки файлов используй `formData`. */
  body?: unknown
  formData?: FormData
  query?: Record<string, QueryValue>
  signal?: AbortSignal
  auth?: AuthMode
}

type ApiEnvelope<T> = { data: T }

type ApiErrorEnvelope = {
  error?: {
    code?: string
    message?: string
    details?: unknown
    action?: string | null
  }
}

function buildUrl(path: string, query?: Record<string, QueryValue>): string {
  const url = `${env.apiBaseUrl}${path}`
  if (!query) return url

  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === '') continue
    search.set(key, String(value))
  }

  const qs = search.toString()
  return qs ? `${url}?${qs}` : url
}

function authHeaders(mode: AuthMode): Record<string, string> {
  if (mode === 'none') return {}

  // ВРЕМЕННО: локально (`npm run dev`) секрет подставляет `vite/dev-login-auth.ts`
  // на стороне Node, и `env.devLoginSecret` здесь пуст — оба режима ниже не мешают
  // обычному потоку. На задеплоенном dev-стенде такого Node-слоя нет, поэтому
  // секрет встроен в бандл и заголовок собирается прямо в браузере.
  if (mode === 'dev-login') {
    return env.devLoginSecret ? { 'X-Dev-Login-Secret': env.devLoginSecret } : {}
  }

  if (mode === 'telegram') {
    if (env.devLoginSecret) {
      return {
        'X-Dev-Login-Secret': env.devLoginSecret,
        'X-Dev-Login-TelegramId': String(getDevUser().id),
      }
    }

    const initDataRaw = getRawInitData()
    return initDataRaw ? { 'X-Telegram-InitData': initDataRaw } : {}
  }

  const token = getAuthToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

async function toApiError(response: Response): Promise<ApiError> {
  let payload: ApiErrorEnvelope | undefined
  try {
    payload = (await response.json()) as ApiErrorEnvelope
  } catch {
    payload = undefined
  }

  return new ApiError({
    status: response.status,
    code: payload?.error?.code ?? 'UNKNOWN_ERROR',
    message: payload?.error?.message ?? `HTTP ${String(response.status)}`,
    details: payload?.error?.details,
    action: payload?.error?.action,
  })
}

/**
 * Единственный способ обратиться к API: прямые `fetch` запрещены.
 * Разворачивает конверт `{ data }` и превращает любую ошибку в `ApiError`.
 */
export async function apiRequest<TResponse>(
  path: string,
  options: ApiRequestOptions = {},
): Promise<TResponse> {
  const { method = 'GET', body, formData, query, signal, auth = 'bearer' } = options

  const response = await fetch(buildUrl(path, query), {
    method,
    signal: signal ?? null,
    headers: {
      ...authHeaders(auth),
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    body: formData ?? (body === undefined ? null : JSON.stringify(body)),
  })

  if (!response.ok) {
    throw await toApiError(response)
  }

  if (response.status === 204) {
    return undefined as TResponse
  }

  const envelope = (await response.json()) as ApiEnvelope<TResponse>
  return envelope.data
}
