---
name: fe-state
description: Клиентское состояние во frontend: выбор между useState, подъёмом в родителя, zustand и кэшем react-query, устройство стора, селекторы, persist и его миграции. Используй, когда нужно где-то хранить состояние, добавить стор или решить, куда положить данные. Triggers: state, zustand, store, useState, persist, localStorage, global state.
---

# Состояние

## Куда положить состояние

Задавай вопросы по порядку, останавливайся на первом «да»:

1. **Это данные с сервера?** → кэш react-query, скилл `fe-data`. Никаких копий в сторе.
2. **Нужно только этому компоненту?** → `useState` / `useReducer`.
3. **Нужно нескольким компонентам одного экрана?** → подними в общего родителя,
   передай пропсами. Контекст — только если пропсы протаскиваются глубже трёх уровней.

> `zustand` — пакет пока не установлен: ни одного стора в проекте нет, поэтому
> зависимость убрана как неиспользуемая. Появится первый стор — `npm i zustand`.

4. **Нужно между экранами и переживает навигацию?** → `zustand`-стор в
   `domains/<домен>/model/`.
5. **Должно выжить перезапуск мини-аппа?** → `zustand` + `persist`.

Стор — последний вариант, не первый.

## Устройство стора

```ts
// src/domains/feed/model/feed-filters-store.ts
import { create } from 'zustand'
import { persist } from 'zustand/middleware'

type FeedFiltersState = {
  ageFrom: number
  ageTo: number
  setAgeRange: (from: number, to: number) => void
  reset: () => void
}

const INITIAL = { ageFrom: 18, ageTo: 45 }

export const useFeedFiltersStore = create<FeedFiltersState>()(
  persist(
    (set) => ({
      ...INITIAL,
      setAgeRange: (ageFrom, ageTo) => set({ ageFrom, ageTo }),
      reset: () => set(INITIAL),
    }),
    { name: 'feed-filters' },
  ),
)
```

Правила:

- Один стор — одна ответственность. `useAppStore` со всем подряд запрещён.
- Имя файла — `<что>-store.ts`, имя хука — `use<Что>Store`.
- Экшены живут внутри стора, компоненты не делают `set` напрямую.
- Начальное состояние — отдельная константа, чтобы `reset` был честным.
- Стор экспортируется через `index.ts` домена, если нужен снаружи.

## Чтение из стора

```ts
const ageFrom = useFeedFiltersStore((s) => s.ageFrom) // ✅ подписка на поле
const { ageFrom, ageTo } = useFeedFiltersStore() // ❌ ререндер на любое изменение
```

Всегда селектор. Нужно несколько полей — несколько вызовов селектора
или `useShallow` из `zustand/react/shallow`.

## persist

- Указывай `name` — это ключ в `localStorage`, он должен быть уникальным.
- `partialize` — сохраняй только то, что реально нужно после перезапуска.
- Меняешь форму сохранённого состояния — поднимай `version` и пиши `migrate`,
  иначе у пользователей останется старый мусор.
- Токены и персональные данные в `persist` не кладём.

## Чего не делать

- Не хранить в сторе то, что можно вычислить из уже имеющихся данных.
- Не хранить состояние формы в сторе — это работа `react-hook-form`.
- Не хранить открытость модалок глобально, если модалка живёт на одном экране.
- Не подписываться на весь стор ради одного поля.
