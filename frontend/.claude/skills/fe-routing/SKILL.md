---
name: fe-routing
description: Роутинг во frontend: TanStack Router, добавление экрана и роута, константы путей, типизированные search params через zod, навигация, связь с нативной кнопкой «Назад». Используй, когда добавляешь или меняешь экран, переход, параметр роута. Triggers: routing, router, new page, navigate, route, search params, link.
---

# Роутинг

`@tanstack/react-router`, code-based роуты, **memory history**.

Почему memory history: Telegram передаёт launch params в `location.hash` и управляет
навигацией нативной кнопкой. URL как источник состояния не используется.
Следствие: перезагрузка страницы возвращает на стартовый экран — это ожидаемо.

## Файлы

```
src/app/router/
  router.ts      инстанс роутера
  route-tree.ts  дерево роутов
  RootLayout.tsx корневая обёртка (safe area, высота вьюпорта)
src/shared/config/routes.ts   константы путей
```

## Новый экран — по шагам

1. Путь в `src/shared/config/routes.ts`:

   ```ts
   export const ROUTES = {
     home: '/',
     matches: '/matches',
     match: '/matches/$matchId',
   } as const
   ```

2. Страница в `src/pages/<экран>/ui/<Экран>Page.tsx` + `index.ts` с реэкспортом.

3. Роут в `route-tree.ts`:

   ```ts
   const matchesRoute = createRoute({
     getParentRoute: () => rootRoute,
     path: ROUTES.matches,
     component: MatchesPage,
   })

   export const routeTree = rootRoute.addChildren([homeRoute, matchesRoute])
   ```

4. `npm run typecheck` — типы путей выводятся из дерева, опечатка не соберётся.

## Навигация

```tsx
const navigate = useNavigate()

void navigate({ to: ROUTES.match, params: { matchId } })
```

- Пути только из `ROUTES`, строки в вызовах не пишем.
- Для ссылок используй `<Link to={ROUTES.matches}>`, а не `navigate` в `onClick`.
- Возврат назад — через нативную кнопку Telegram (скилл `fe-telegram`),
  а не `history.back()`.

## Search params

Валидируй схемой — тогда они типизированы и защищены от мусора:

```ts
const feedRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.feed,
  validateSearch: z.object({
    filter: z.enum(['all', 'nearby']).default('all'),
  }),
  component: FeedPage,
})

// в компоненте
const { filter } = feedRoute.useSearch()
```

Search params — для состояния, которое влияет на отображение экрана (фильтр, таб).
Долгоживущие настройки пользователя — в zustand (скилл `fe-state`).

## Загрузка данных

Данные грузим хуками react-query внутри страницы, а не в `loader` роута —
кэш и состояния загрузки остаются в одном месте. `loader` используем только если
понадобится блокировать переход до готовности данных (обсудить отдельно).

## Что должно быть на каждом экране

- Заголовок и нативная кнопка «Назад», если экран не корневой.
- Состояния загрузки / пусто / ошибка (скилл `fe-component`).
- Обёртка корневого лэйаута уже даёт safe area — не дублируй `pt-safe`.
