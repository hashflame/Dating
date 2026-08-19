# Блізка (Blizka) — Backend

Backend дейтинг-продукта в формате Telegram Mini App. .NET 10 / ASP.NET Core Web API, PostgreSQL 16 + PostGIS.

Это пакет `backend/` монорепозитория `dating` (соседний `frontend/` — пока заглушка без кода, см. [корневой README](../README.md)). Все пути и команды ниже — относительно `backend/`.

## Архитектура

Четыре слоя, зависимости направлены внутрь, к `App`:

```
Blizka.Host  →  Blizka.Api, Blizka.App, Blizka.Data
Blizka.Api   →  Blizka.App
Blizka.Data  →  Blizka.App
Blizka.App   →  (ни от кого не зависит)
```

- **Blizka.App** — доменные сущности, enum'ы, интерфейсы, use-case'ы (MediatR), валидация (FluentValidation). Ядро без ссылок на ASP.NET Core/EF Core.
- **Blizka.Data** — EF Core (`BlizkaDbContext`, Npgsql + PostGIS), конфигурации сущностей, seed-данные, репозитории.
- **Blizka.Api** — класс-библиотека: контроллеры и DTO, подключается к хосту через `AddApplicationPart`.
- **Blizka.Host** — точка входа (composition root): конфигурация, Serilog, CORS, Quartz, DI-регистрация слоёв.
- **Blizka.UnitTests** / **Blizka.IntegrationTests** — тесты.

Подробнее — в [`CLAUDE.md`](CLAUDE.md#architecture).

## Быстрый старт

```bash
# восстановить зависимости / собрать / прогнать тесты
dotnet restore
dotnet build
dotnet test

# поднять локальный Postgres (с PostGIS) и MinIO
docker compose up -d postgres minio minio-init

# применить миграции БД
dotnet ef database update --project src/Blizka.Data --startup-project src/Blizka.Host
# ...либо через контейнерный мигратор, без локального dotnet-ef:
docker compose up --build migrator

# запустить API
dotnet run --project src/Blizka.Host
```

После запуска в Development доступен Scalar UI (`/scalar/v1`) и `GET /api/health`.

Полный список команд (включая создание миграций, запуск отдельного теста) — в [`CLAUDE.md`](CLAUDE.md#commands).

## Документация

- [`CLAUDE.md`](CLAUDE.md) — гайд по репозиторию: команды, архитектура, договорённости, язык комментариев.
- [`decomposition.md`](decomposition.md) — декомпозиция задач (эпики T-0.x…T-21.x), источник истины по объёму MVP.
- [`docs/specs/`](docs/specs/README.md) — спецификации фич вне исходной декомпозиции.
- [`docs/deployment/railway.md`](docs/deployment/railway.md) — деплой на Railway.

## Стек

- .NET 10, ASP.NET Core Web API (Minimal hosting + MVC-контроллеры)
- PostgreSQL 16 + PostGIS, EF Core (Npgsql)
- MediatR, FluentValidation
- Serilog, Quartz
- MinIO / S3-совместимое хранилище фото
- Аутентификация — Telegram `initData` + JWT

## Тестирование

- `Blizka.UnitTests` — без БД и HTTP, фейковые репозитории.
- `Blizka.IntegrationTests` — через `WebApplicationFactory<Program>` / `TestServer`.

```bash
dotnet test
dotnet test tests/Blizka.UnitTests --filter "FullyQualifiedName~ClassName.MethodName"
```
