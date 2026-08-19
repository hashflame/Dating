# Блізка (Blizka)

Дейтинг-продукт в формате Telegram Mini App.

## Структура монорепо

```
dating/
├── backend/    — .NET 10 / ASP.NET Core Web API, PostgreSQL 16 + PostGIS
└── frontend/   — Telegram Mini App: React 19, Vite, Tailwind v4, TanStack Router/Query
```

Каждый пакет самодостаточен и живёт в своей папке. Подробности по конкретному пакету — в его собственном README/CLAUDE.md:

- [`backend/README.md`](backend/README.md) — запуск, команды, зависимости.
- [`backend/CLAUDE.md`](backend/CLAUDE.md) — архитектура, соглашения, гайд для работы с кодом.
- [`backend/decomposition.md`](backend/decomposition.md) — декомпозиция задач backend (эпики T-0.x…T-21.x), источник истины по объёму MVP.
- [`frontend/README.md`](frontend/README.md) — запуск, в том числе внутри Telegram.
- [`frontend/CLAUDE.md`](frontend/CLAUDE.md) — стек, структура, соглашения фронтенда.

## Frontend

Telegram Mini App на React + Vite. Архитектура — слои `app / pages / widgets / domains / shared`,
направление импортов проверяется линтером. Тема приложения наследуется от Telegram-клиента.

```bash
cd frontend
npm install
cp .env.example .env
npm run dev
```

Документы разработки фронтенда — в [`frontend/docs/`](frontend/docs):
`prd.md`, `architecture.md`, `ux-spec.md`, `api-gaps.md`, `stories/`.

## Работа с AI-агентом

Правила для агента лежат в [`frontend/.claude/skills/`](frontend/.claude/skills):

- `fe-*` — правила написания кода фронтенда.
- `bmad-*` — процесс разработки (роли Analyst → PM → Architect → SM → Dev → QA).

Общая точка входа — [`CLAUDE.md`](CLAUDE.md), детали по фронтенду — [`frontend/CLAUDE.md`](frontend/CLAUDE.md).

Как ставить задачи агенту (для человека, а не для агента) —
[`frontend/docs/Obuchalka Sashki Vaibkodingu.md`](<frontend/docs/Obuchalka Sashki Vaibkodingu.md>).

## С чего начать

Backend — команды разработки описаны в [`backend/CLAUDE.md`](backend/CLAUDE.md#commands).
Frontend — команды и соглашения в [`frontend/CLAUDE.md`](frontend/CLAUDE.md#команды).
