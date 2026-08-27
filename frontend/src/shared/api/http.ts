import i18next from 'i18next'

import { env } from '@/shared/config'
import { getDevUser } from '@/shared/telegram'
import { getRawInitData } from '@/shared/telegram/bridge'

import { ApiError } from './api-error'
import { getAuthToken } from './auth-token'

type QueryValue = string | number | boolean | undefined | null

/** `telegram` — initData в заголовке, его принимает только `POST /api/auth/telegram`. */
type AuthMode = 'bearer' | 'telegram' | 'none'

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

/**
 * `?locale=` побеждает всё остальное на сервере (`RequestLocaleResolver`: query >
 * Accept-Language > JWT-claim > дефолт), поэтому шлём его на каждый запрос сами —
 * иначе локаль ответа определяет браузерный `Accept-Language` или язык
 * Telegram-профиля на момент входа, а не язык, который человек выбрал в самом
 * интерфейсе (`i18next.language`, тот же переключатель RU/BE/EN в дев-панели).
 * Без этого вопрос дня, подписи в кошельке, идеи свидания и тексты ошибок могли
 * прийти не на том языке, что интерфейс.
 */
function buildUrl(path: string, query?: Record<string, QueryValue>): string {
  const url = `${env.apiBaseUrl}${path}`

  const search = new URLSearchParams()
  search.set('locale', i18next.language)
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value === undefined || value === null || value === '') continue
    search.set(key, String(value))
  }

  return `${url}?${search.toString()}`
}

function authHeaders(mode: AuthMode): Record<string, string> {
  if (mode === 'none') return {}

  if (mode === 'telegram') {
    // ВРЕМЕННО (спека 003 в backend): вход по демо-аккаунту вместо подписи.
    // Локально (`npm run dev`) секрет подставляет `vite/dev-login-auth.ts` на
    // стороне Node, и `env.devLoginSecret` здесь пуст. На задеплоенном
    // dev-стенде такого Node-слоя нет, поэтому секрет встроен в бандл и
    // заголовок собирается прямо в браузере.
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

  // Тела нет у 204 (DELETE, блокировки) и у 202 (data-export — архив собирается
  // в фоне): response.json() на пустой строке бросил бы SyntaxError.
  const text = await response.text()
  if (text === '') {
    return undefined as TResponse
  }

  const envelope = JSON.parse(text) as ApiEnvelope<TResponse>
  return envelope.data
}
