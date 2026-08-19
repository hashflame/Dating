import { retrieveRawInitData } from '@tma.js/sdk-react'

import { env } from '@/shared/config'

import { ApiError } from './api-error'

type QueryValue = string | number | boolean | undefined | null

export type ApiRequestOptions = {
  method?: 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE'
  /** JSON-тело запроса. Для загрузки файлов используй `formData`. */
  body?: unknown
  formData?: FormData
  query?: Record<string, QueryValue>
  signal?: AbortSignal
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

/**
 * Заголовок авторизации Telegram Mini App.
 * Вне Telegram (или до инициализации SDK) возвращает пустой объект.
 */
function authHeaders(): Record<string, string> {
  try {
    const initDataRaw = retrieveRawInitData()
    return initDataRaw ? { Authorization: `tma ${initDataRaw}` } : {}
  } catch {
    return {}
  }
}

async function parseError(response: Response): Promise<ApiError> {
  let payload: unknown
  try {
    payload = await response.json()
  } catch {
    payload = undefined
  }

  const shape = payload as { message?: string; code?: string; title?: string } | undefined

  return new ApiError({
    status: response.status,
    message: shape?.message ?? shape?.title ?? `HTTP ${String(response.status)}`,
    code: shape?.code,
    details: payload,
  })
}

/**
 * Единственный способ обратиться к API. Прямые вызовы `fetch` в коде запрещены.
 *
 * Возвращает распарсенный JSON. Формат конверта ответа (data/errors/pagination)
 * задан на бэкенде — при первом реальном эндпоинте сверься с `backend/` и,
 * если конверт есть, разворачивай его здесь в одном месте.
 */
export async function apiRequest<TResponse>(
  path: string,
  options: ApiRequestOptions = {},
): Promise<TResponse> {
  const { method = 'GET', body, formData, query, signal } = options

  const response = await fetch(buildUrl(path, query), {
    method,
    signal: signal ?? null,
    headers: {
      ...authHeaders(),
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    body: formData ?? (body === undefined ? null : JSON.stringify(body)),
  })

  if (!response.ok) {
    throw await parseError(response)
  }

  if (response.status === 204) {
    return undefined as TResponse
  }

  return (await response.json()) as TResponse
}
