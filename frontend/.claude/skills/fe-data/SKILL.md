---
name: fe-data
description: Серверные данные во frontend: react-query v5, ключи кэша, хуки запросов и мутаций, инвалидация, оптимистичные обновления, apiRequest, обработка ApiError, пагинация. Используй, когда добавляешь или правишь любой запрос к API, мутацию, кэш. Triggers: react-query, useQuery, useMutation, fetch, API request, cache, invalidate, pagination.
---

# Серверные данные

`@tanstack/react-query` v5. Настройки по умолчанию — `src/app/providers/query-client.ts`.

## Слои

```
domains/<домен>/api/
  <домен>-keys.ts     ключи кэша
  get-<сущность>.ts   функция запроса (без react)
  use-<сущность>.ts   хук react-query
```

Функция запроса не знает про react, хук не знает про `fetch`. Так запрос можно
переиспользовать и подменять заглушкой, не трогая хук.

## HTTP

Только `apiRequest` из `@/shared/api`. Прямой `fetch` в коде запрещён.

```ts
import { apiRequest } from '@/shared/api'

export function getMatches(signal?: AbortSignal): Promise<Match[]> {
  return apiRequest<Match[]>('/matches', { signal })
}

export function sendLike(userId: string): Promise<void> {
  return apiRequest<void>('/likes', { method: 'POST', body: { userId } })
}
```

`apiRequest` сам подставляет заголовок авторизации Telegram и бросает `ApiError`.
Эндпоинта ещё нет — скилл `fe-api-contract`.

## Ключи кэша

Один домен — один объект ключей, чтобы инвалидация была предсказуемой:

```ts
export const matchesKeys = {
  root: ['matches'] as const,
  list: (filters?: MatchFilters) => [...matchesKeys.root, 'list', filters ?? {}] as const,
  detail: (id: string) => [...matchesKeys.root, 'detail', id] as const,
}
```

Правила:

- Ключ строится только через этот объект, массивы руками не собираем.
- Всё, что влияет на результат (фильтры, id, страница), входит в ключ.
- Инвалидация домена — `queryClient.invalidateQueries({ queryKey: matchesKeys.root })`.

## Хуки запросов

```ts
export function useMatches(filters?: MatchFilters): UseQueryResult<Match[], Error> {
  return useQuery({
    queryKey: matchesKeys.list(filters),
    queryFn: ({ signal }) => getMatches(signal),
  })
}
```

- Возвращай результат `useQuery` целиком, не разбирай его в хуке.
- `staleTime`/`gcTime` переопределяй только с комментарием, почему эти данные другие.
- Зависимый запрос — `enabled`, а не условный вызов хука.
- Бесконечный список — `useInfiniteQuery` с `getNextPageParam` по курсору из ответа.

## Мутации

```ts
export function useSendLike() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: sendLike,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: feedKeys.root })
      void queryClient.invalidateQueries({ queryKey: matchesKeys.root })
    },
  })
}
```

- После мутации инвалидируй всё, что могло измениться — включая соседние домены.
  Импортируй их ключи через публичный API: `import { matchesKeys } from '@/domains/matches'`.
- Оптимистичное обновление добавляй только там, где задержка реально мешает
  (свайпы ленты). Обязательно с `onError` → откат через `setQueryData`.

## Ошибки

```ts
import { isApiError } from '@/shared/api'

if (isApiError(error) && error.isUnauthorized) {
  /* … */
}
```

- Повторные попытки уже настроены глобально: 4xx не повторяются, 5xx — до двух раз.
- В UI показывай сообщение из `i18n`, а не `error.message` — тексты API не переведены.
- Обработку ошибки конкретного запроса делай в компоненте, не в хуке.

## Что не делать

- Не дублируй серверные данные в zustand. Источник истины — кэш react-query.
- Не вызывай `refetch` в `useEffect` — используй инвалидацию.
- Не отключай `refetchOnWindowFocus` локально: он уже выключен глобально
  (внутри Telegram фокус срабатывает непредсказуемо).
