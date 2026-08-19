---
name: fe-architecture
description: 'Размещение кода во frontend: слои app/pages/widgets/domains/shared, слайсы, сегменты api/model/types/ui/lib, публичный API через index.ts, где лежат переиспользуемые компоненты, хуки и утилиты. Используй ВСЕГДА перед созданием нового файла, компонента, страницы, домена, хука или утилиты, при вопросе «куда это положить» и при разборе ошибки boundaries/dependencies. Triggers: architecture, FSD, folder structure, where to put file, new page, new domain, layers, imports, public API.'
---

# Архитектура frontend

Работает только в `frontend/`. Проверяется командой `npm run lint`.

## Слои

```
src/app/       инициализация: main.tsx, провайдеры, роутер, стили
src/pages/     экраны
src/widgets/   составные блоки, переиспользуемые между экранами
src/domains/   предметные области
src/shared/    переиспользуемое без привязки к предметной области
```

Импорт только вниз: `app` → `pages` → `widgets` → `domains` → `shared`.
Внутри одного слоя импорт между слайсами разрешён только для `widgets` и `domains`.

## Правило нуля: сначала ищи существующее

Перед созданием любого файла проверь, нет ли уже готового решения.

```bash
cd frontend
ls src/shared/lib src/shared/hooks src/shared/ui        # утилиты, хуки, компоненты
grep -rn "export function" src/shared/lib src/shared/hooks
ls src/domains                                          # существующие домены
```

Дублирующая утилита или хук — ошибка, а не «мелочь». Если нужное почти есть —
расширь существующее (добавь параметр, вынеси общую часть), а не копируй рядом.

## Куда положить новый код

| Что                                      | Куда                        | Пример                           |
| ---------------------------------------- | --------------------------- | -------------------------------- |
| Переиспользуемый компонент без домена    | `shared/ui/<Имя>.tsx`       | `EmptyState`, `Spinner`          |
| Примитив в стиле shadcn                  | `shared/ui/kit/<имя>.tsx`   | `button`, `input`, `skeleton`    |
| Переиспользуемая чистая функция          | `shared/lib/<тема>.ts`      | `date.ts`, `number.ts`           |
| Переиспользуемый хук                     | `shared/hooks/use-<имя>.ts` | `use-debounced-value.ts`         |
| Работа с сетью                           | `shared/api/`               | `apiRequest`, `ApiError`, `stub` |
| Конфиг, окружение, пути роутов           | `shared/config/`            | `env.ts`, `routes.ts`            |
| Тексты и локали                          | `shared/i18n/`              | `locales/ru/common.json`         |
| Обёртки над Telegram SDK                 | `shared/telegram/`          | `useBackButton`, `useHaptic`     |
| Общие типы-утилиты                       | `shared/types/`             | `Nullable`, `RequireKeys`        |
| Всё про предметную область               | `domains/<домен>/`          | `feed`, `matches`, `wallet`      |
| Экран под роутом                         | `pages/<экран>/`            | `home`, `onboarding`             |
| Блок из нескольких доменов на 2+ экранах | `widgets/<виджет>/`         | `bottom-nav`                     |

Если блок нужен на одном экране — оставь его в `pages/<экран>/ui/`, виджет не создавай.

## Почему типы и в domains, и в shared

Это разные вещи, объединять их нельзя:

- `domains/<домен>/types/` — типы предметной области: `Viewer`, `Match`, `Photo`.
  Они принадлежат домену и живут рядом с его запросами. Общая папка типов
  превратилась бы в свалку, которую импортируют все и от которой ничего не отвязать.
- `shared/types/` — типы-утилиты без предметного смысла: `Nullable<T>`, `RequireKeys<T, K>`.
  Они не про дейтинг, а про TypeScript.

Тот же принцип у остальных сегментов: `shared/api` — транспорт (один `apiRequest`,
класс ошибки, заглушки), `domains/<домен>/api` — конкретные запросы этого домена.
Транспорт один на приложение, запросы принадлежат домену.

## Сегменты внутри слайса

```
src/domains/matches/
  api/      запросы и react-query хуки, ключи кэша
  model/    zustand-стор, селекторы, чистая бизнес-логика
  types/    типы домена и DTO
  ui/       компоненты домена
  lib/      чистые хелперы, нужные только этому домену
  index.ts  публичный API — обязателен
```

Пустые сегменты не создавай. Появился второй файл в сегменте — заведи папку,
до этого достаточно одного файла.

Хелпер нужен двум доменам — переезжает в `shared/lib`, а не копируется.

## Публичный API

`index.ts` слайса — единственная точка входа снаружи:

```ts
// src/domains/matches/index.ts
export { useMatches } from './api/use-matches'
export { matchesKeys } from './api/matches-keys'
export { MatchCard } from './ui/MatchCard'
export type { Match } from './types/match'
```

- Реэкспортируй только то, что реально нужно снаружи.
- Не делай `export * from`.
- Не реэкспортируй внутренние хелперы «на будущее».

## Импорты

```ts
import { useMatches } from '@/domains/matches' // ✅ через публичный API
import { cn } from '@/shared/lib' // ✅ shared можно глубоко
import { formatAge } from '../lib/format-age' // ✅ внутри своего слайса

import { Match } from '@/domains/matches/types/match' // ❌ обход index.ts
import { useViewer } from '@/domains/viewer' // ❌ если это файл в shared
import { Card } from '../../../shared/ui' // ❌ используй @/
```

## Новый домен — по шагам

```bash
mkdir -p frontend/src/domains/<домен>/{api,types,ui}
```

1. `types/<сущность>.ts` — тип. Поля сверь с `backend/` (скилл `fe-api-contract`).
2. `api/<домен>-keys.ts` — ключи кэша.
3. `api/get-<сущность>.ts` — запрос через `apiRequest` или `stub`.
4. `api/use-<сущность>.ts` — хук react-query.
5. `ui/` — компоненты.
6. `index.ts` — публичный API.
7. `npm run lint` — убедиться, что границы не нарушены.

Живой образец, с которого можно копировать: `frontend/src/domains/viewer/`.

## Новый экран — по шагам

См. скилл `fe-routing`: путь в `shared/config/routes.ts` → страница в
`pages/<экран>/` → роут в `app/router/route-tree.ts`.

## Куда НЕ надо

- Общие `types.ts` или `utils.ts` в корне `src` — типы и хелперы живут в своём слайсе
  или в `shared` с осмысленным именем файла по теме (`date.ts`, `number.ts`).
- Папка `utils/` со всем подряд — у нас это `shared/lib`, разбитый по темам.
- Слой `widgets` «на всякий случай» — создавай, когда появился второй потребитель.
- Барели `index.ts` внутри сегментов ради красоты — только публичный API слайса.
- Копия существующего хелпера с другим именем.
