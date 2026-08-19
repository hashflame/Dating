---
name: fe-done-check
description: Чеклист самопроверки перед сдачей задачи по frontend: npm run check, проверка слоёв, состояний экрана, токенов, локализации, заглушек API, документации, формат отчёта. Используй ВСЕГДА перед тем, как сообщить, что задача по frontend готова. Triggers: done, finished, ready, self-review, checklist, before commit.
---

# Проверка перед сдачей

Пройди пункты по порядку. Не сообщай о готовности, пока все не закрыты.

## 1. Прогон

```bash
cd frontend
npm run check
```

Это typecheck + lint + format:check. Всё должно быть зелёным.
Красный `boundaries/dependencies` — нарушена архитектура, читай `fe-architecture`.

Если менялась сборка или добавлялись зависимости, дополнительно:

```bash
npm run build
```

## 2. Архитектура

- Новый код лежит в правильном слое.
- Слайсы импортируются через `index.ts`.
- В `index.ts` нет лишних реэкспортов «на будущее».
- Нет `import.meta.env` вне `shared/config/env.ts`. Исключение — `import.meta.env.DEV`
  как признак сборки, чтобы dev-код не попал в релиз.
- Нет `fetch` вне `shared/api/http.ts`.
- Нет импортов `@tma.js/*` вне `shared/telegram`.

Быстрая проверка:

```bash
grep -rn "import.meta.env" src --include=*.ts --include=*.tsx | grep -v "shared/config/env.ts" | grep -v "import.meta.env.DEV"
grep -rn "fetch(" src --include=*.ts --include=*.tsx | grep -v "shared/api" | grep -v "refetch("
grep -rn "@tma.js" src --include=*.ts --include=*.tsx | grep -v "shared/telegram"
```

## 3. Переиспользование и мёртвый код

- Нет заново написанной утилиты, хука или компонента, который уже есть в `shared/`
  (таблица готового — в `fe-code-style`).
- Нет собственного форматирования дат, чисел и форм слов вместо `Intl` и i18n.
- Нет скопированного блока верстки в третьем месте.
- Нет файлов и экспортов без потребителя: правило первого потребителя в `fe-architecture`.
- В `index.ts` слайса нет реэкспортов «на будущее».

Мёртвые экспорты (символ встречается только в своём файле):

```bash
for sym in $(grep -rhoE 'export (function|const|type|class) [A-Za-z0-9_]+' src --include=*.ts --include=*.tsx | grep -v 'ui/kit' | awk '{print $3}' | sort -u); do
  n=$(grep -rlE "\b$sym\b" src --include=*.ts --include=*.tsx | grep -v 'ui/kit' | wc -l)
  [ "$n" -le 1 ] && echo "$sym"
done
```

Реэкспорты слайса без внешних потребителей:

```bash
for sym in $(grep -oE '\b[A-Za-z][A-Za-z0-9_]*\b' src/domains/<домен>/index.ts | sort -u); do
  n=$(grep -rlE "\b$sym\b" src --include=*.ts --include=*.tsx | grep -v 'src/domains/<домен>' | wc -l)
  [ "$n" -eq 0 ] && echo "$sym"
done
```

Зависимости, которых нет в коде:

```bash
node -e "const p=require('./package.json');for(const d of Object.keys({...p.dependencies,...p.devDependencies}))if(!require('node:child_process').execSync('grep -rl \"'+d+'\" src vite.config.ts eslint.config.js 2>/dev/null || true').toString().trim())console.log(d)"
```

Найденное либо получает потребителя в этой задаче, либо удаляется.

## 4. UI

- У каждого экрана и блока с запросом есть загрузка, пусто и ошибка.
- Цвета только из токенов: нет `bg-white`, `dark:`, `bg-[#…]`.
- Новая верстка проверена в трёх положениях переключателя темы: система, светлая, тёмная.
- Нижние липкие панели с `pb-safe`, высота через `min-h-viewport`.
- Кликабельные зоны не меньше `size-11`.
- Нативная кнопка «Назад» показывается там, где есть куда возвращаться.

## 5. Тексты

- Нет захардкоженного текста в JSX.
- Новые ключи добавлены во все три языка (`ru`, `be`, `en`).

## 6. API

- Каждая заглушка помечена `// @stub:` и присутствует в `docs/api-gaps.md`.
- Заглушки, для которых эндпоинт уже появился, заменены на `apiRequest`.
- Типы DTO сверены с `backend/`.

## 7. Документация

- Изменилась структура или принято архитектурное решение — обновлён `docs/architecture.md`.
- Работал по истории — заполнен раздел «Отчёт разработчика» в файле истории.

## 8. Отчёт

В ответе перечисли:

- что сделано (по критериям приёмки истории, если она была);
- какие файлы созданы и изменены;
- что осталось незакрытым и почему;
- какие заглушки поставлены.

Не пиши «готово», если хоть один пункт не выполнен — напиши, что именно не закрыто.
