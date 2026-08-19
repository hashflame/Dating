# Блізка — Frontend

Telegram Mini App. React SPA, работает внутри Telegram-клиента.

## Обязательный порядок работы

1. **Перед написанием кода** открой скиллы по таблице ниже. Минимум всегда:
   `fe-architecture` (куда положить) и `fe-code-style` (как писать).
2. **Перед новой утилитой, хуком, компонентом** проверь, нет ли готового в
   `src/shared/lib`, `src/shared/hooks`, `src/shared/ui`. Дублировать нельзя.
3. **Перед новым запросом к API** сверь контракт с папкой `backend/` — скилл `fe-api-contract`.
4. **Перед словами «готово»** пройди скилл `fe-done-check` и прогони `npm run check`.

## Команды

```bash
npm run dev          # dev-сервер (http://localhost:5173)
npm run dev:https    # то же по https — нужно для запуска внутри Telegram
npm run typecheck    # tsc
npm run lint         # eslint (включая проверку архитектуры)
npm run lint:fix     # eslint --fix
npm run format       # prettier --write
npm run build        # прод-сборка
npm run check        # typecheck + lint + format:check — обязателен перед сдачей задачи
```

Примитивы UI добавляются так: `npx shadcn@latest add <имя> --yes`.

## Стек

| Задача           | Инструмент                                            |
| ---------------- | ----------------------------------------------------- |
| UI               | React 19 + TypeScript (strict)                        |
| Сборка           | Vite                                                  |
| Роутинг          | `@tanstack/react-router` (code-based, memory history) |
| Серверные данные | `@tanstack/react-query` v5                            |
| Клиентский стейт | `zustand` (+ `persist`)                               |
| Формы            | `react-hook-form` + `zod`                             |
| Стили            | Tailwind CSS v4 (css-first, без `tailwind.config`)    |
| Компоненты       | shadcn/ui в `src/shared/ui/kit`                       |
| Telegram         | `@tma.js/sdk-react`                                   |
| Анимации         | `motion`                                              |
| Локализация      | `i18next` + `react-i18next` (ru, be, en)              |
| Иконки           | `lucide-react`                                        |
| Архитектура      | `eslint-plugin-boundaries`                            |

## Структура

```
src/
  app/        инициализация: main.tsx, провайдеры, роутер, стили
    dev/      dev-инструменты, вырезаются из production-сборки
  pages/      экраны, по одному слайсу на экран
  widgets/    составные блоки, переиспользуемые между экранами
  domains/    предметные области: feed, matches, profile, wallet, viewer…
  shared/     переиспользуемое без привязки к предметной области
    api/      apiRequest, ApiError, stub
    config/   env, пути роутов
    hooks/    useDebouncedValue, useDisclosure, useFormatters
    i18n/     инстанс i18next и локали ru/be/en
    lib/      cn, date, number — чистые утилиты по темам
    telegram/ обёртки над Telegram SDK
    types/    типы-утилиты (Nullable, RequireKeys)
    ui/       Button, Input, Skeleton, Spinner, EmptyState, ErrorState
    ui/kit/   примитивы shadcn (не править стиль кода)
```

Внутри слайса — сегменты `api/`, `model/`, `types/`, `ui/`, `lib/` и обязательный `index.ts`.

Правила импортов (проверяет `npm run lint`):

- Импорт только «вниз»: `app` → `pages` → `widgets` → `domains` → `shared`.
- Слайс (`pages/*`, `widgets/*`, `domains/*`) импортируется **только** через свой `index.ts`.
- Внутрь `shared` можно импортировать глубоко: `@/shared/lib/cn`.
- Внутри своего слайса — относительные пути (`../types/viewer`), наружу — алиас `@/`.

Разбор случаев и объяснение, почему типы лежат и в `domains`, и в `shared` —
скилл `fe-architecture`.

## Скиллы

Лежат в `frontend/.claude/skills/`. Подхватываются автоматически по смыслу задачи,
называть их в запросе не нужно.

| Задача                                            | Скилл                |
| ------------------------------------------------- | -------------------- |
| Куда положить код, новый домен, страница, утилита | `fe-architecture`    |
| Написать/разбить компонент, папка компонента      | `fe-component`       |
| Верстка, Tailwind, токены темы                    | `fe-styles`          |
| Почистить разъехавшиеся стили                     | `fe-styles-refactor` |
| Именование, типы, экспорты, переиспользование     | `fe-code-style`      |
| Запросы, кэш, мутации                             | `fe-data`            |
| Узнать контракт API, заглушить нереализованное    | `fe-api-contract`    |
| Клиентское состояние                              | `fe-state`           |
| Формы и валидация                                 | `fe-forms`           |
| Telegram SDK, кнопки, тема, хаптика               | `fe-telegram`        |
| Роуты и навигация                                 | `fe-routing`         |
| Тексты и переводы                                 | `fe-i18n`            |
| Тормоза, ререндеры, размер бандла                 | `fe-performance`     |
| Проверка перед сдачей задачи                      | `fe-done-check`      |
| Процесс: новая фича или эпик, роли, истории       | `bmad` и `bmad-*`    |

## Документы разработки

```
docs/
  Obuchalka Sashki Vaibkodingu.md   как писать промпт агенту (для человека)
  prd.md            что делаем и зачем: бриф, эпики, истории
  architecture.md   как это устроено технически и почему
  ux-spec.md        экраны, навигация, состояния
  api-gaps.md       чего не хватает от API (заглушки)
  stories/          истории; TEMPLATE.md — шаблон
```

## Что нельзя

- `fetch` напрямую — только `apiRequest` из `@/shared/api`.
- `window.Telegram` и импорт `@tma.js/*` вне `@/shared/telegram`.
- `import.meta.env` вне `@/shared/config/env.ts` — кроме `import.meta.env.DEV`
  как признака сборки для отрезания dev-кода.
- Хардкод текста в JSX — только через `t()`.
- Цвета вне токенов: `bg-white`, `dark:`, `bg-[#…]`.
- `any`, `!` (non-null assertion), `export default`.
- Свой стиль кода в `src/shared/ui/kit/` — там живёт вывод `npx shadcn add`.
- Дубль утилиты, хука или компонента, который уже есть в `shared`.
- Менять файлы в `backend/` — оттуда только читаем контракт API.
