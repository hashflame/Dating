# Блізка (Blizka)

Дейтинг-продукт в формате Telegram Mini App.

## Структура монорепо

```
dating/
├── backend/    — .NET 10 / ASP.NET Core Web API, PostgreSQL 16 + PostGIS
└── frontend/   — заглушка, кода пока нет
```

Каждый пакет самодостаточен и живёт в своей папке. Подробности по конкретному пакету — в его собственном README/CLAUDE.md:

- [`backend/README.md`](backend/README.md) — запуск, команды, зависимости.
- [`backend/CLAUDE.md`](backend/CLAUDE.md) — архитектура, соглашения, гайд для работы с кодом.
- [`backend/decomposition.md`](backend/decomposition.md) — декомпозиция задач backend (эпики T-0.x…T-21.x), источник истины по объёму MVP.

## Frontend

`frontend/` — пока пустой пакет-заглушка (`.gitkeep`), кода нет. Появится отдельным пакетом монорепо, когда стартует его разработка.

## С чего начать

Backend — единственный пакет с кодом на данный момент. Все команды разработки (restore/build/test/run, миграции, Docker) описаны в [`backend/CLAUDE.md`](backend/CLAUDE.md#commands).
