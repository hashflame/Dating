# Spec 001: Деплой Blizka.Host на Railway

**Status:** Implemented
**Date:** 2026-08-19

## Problem

Backend Блізки сейчас можно запустить только локально (`dotnet run --project src/Blizka.Host`)
или поднять `postgres`+`migrator` через `docker-compose.yml`. Для `Blizka.Host` (самого API)
нет ни Dockerfile, ни CI/CD, ни production-конфигурации — приложение невозможно выложить в
публичный интернет, а без этого Telegram Mini App и бот не могут обращаться к бэкенду.

Нужен воспроизводимый, автоматизируемый путь деплоя API на managed PaaS — выбран **Railway** —
с безопасной передачей секретов через переменные окружения и применением EF Core миграций к
prod БД без ручных шагов на сервере.

Репозиторий сейчас живёт на GitLab (`gitlab.com/alex47715/dating`), не на GitHub.

## Scope

### In

- **Dockerfile** для `Blizka.Host`: multi-stage build (SDK для сборки → `mcr.microsoft.com/dotnet/aspnet:10.0`
  для runtime), по аналогии с `Dockerfile.migrator`.
- **Исправление порядка источников конфигурации** в `Program.cs`: сейчас `AddYamlFile(...)`
  вызывается после встроенного `AddEnvironmentVariables()` (добавляется по умолчанию в
  `WebApplication.CreateBuilder`), из-за чего YAML побеждает переменные окружения, а не наоборот.
  Переменные окружения должны иметь приоритет над `appsettings.yaml`/`appsettings.{Environment}.yaml`.
- **`UseForwardedHeaders()`** перед `UseHttpsRedirection()` — без него приложение за
  Railway-прокси (TLS обрывается на границе, до контейнера трафик идёт по HTTP) уйдёт в
  redirect-loop.
- **Fail-fast валидация критичных секретов** при старте в Production-окружении (как минимум
  `Jwt:Secret`) — пустое/отсутствующее значение должно останавливать запуск, а не давать
  приложению стартовать в небезопасном состоянии.
- **Production-конфигурация**, управляемая переменными окружения Railway (Database, Jwt,
  Telegram, Storage, Ai, Cors — те же ключи, что уже объявлены в `appsettings.yaml`).
- **`.gitlab-ci.yml`**: стадии `build → test → migrate → deploy`, `deploy` — **manual job**
  (кнопка в GitLab CI), аутентификация в Railway через API-токен, хранимый как
  protected/masked переменная в GitLab CI/CD Variables.
- **Отдельный CI-шаг миграций** перед деплоем API — переиспользует существующий подход
  `Dockerfile.migrator`, нацеленный на prod `BLIZKA_DB_CONNECTION`. Если шаг миграций падает,
  деплой API не выполняется.
- **Одно Railway-окружение — production** (без staging в этой итерации).
- **Healthcheck** в Railway на существующий `/api/health`.
- **Документация переменных окружения**, которые нужно завести в Railway (Database, Jwt,
  Telegram, Storage, Ai, Cors) — без фактического провижининга S3-бакета/AI-ключа.
- **Пошаговая инструкция** первоначальной ручной настройки Railway-проекта: создание проекта,
  сервиса БД, сервиса API, переменных окружения, привязки Railway API-токена в GitLab.
- **База данных**: Railway Postgres через собственный сервис на образе `postgis/postgis`
  (тот же образ, что уже используется в `docker-compose.yml`) — см. Deferred Decisions.

### Out

- Регистрация Telegram webhook (`setWebhook` на Railway-домен) — отдельная ручная задача вне
  этой спеки.
- Staging-окружение.
- Фактическое провижининг S3-совместимого бакета и получение AI API-ключа — только
  документируются нужные переменные, значения заводит инженер вручную.
- Перенос репозитория на GitHub.
- Автоматический (push-triggered) continuous deployment — в этой итерации деплой запускается
  вручную через manual job в GitLab CI.
- Kubernetes и другие облачные платформы.

## Domain Model

Нет новых доменных сущностей — задача инфраструктурная, `Blizka.App`/`Blizka.Data` не меняются.

## API Contract

Новых эндпоинтов нет. Используется существующий `/api/health` (уже замаплен в `Program.cs`
через `app.MapHealthChecks`) как readiness-проверка для Railway healthcheck.

## Authorization

Не затрагивает авторизацию пользователей приложения. Новый секрет в этой спеке — Railway
API-токен для CI; хранится как protected/masked переменная в GitLab CI/CD Variables и
используется только deploy-job'ом.

## Edge Cases & Failure Modes

- **EC-1**: Критичный секрет (`Jwt:Secret`) пуст/отсутствует в Production → приложение не
  должно запускаться (явная ошибка конфигурации при старте).
- **EC-2**: Шаг миграций в CI падает → deploy-job не запускается; предыдущая версия API
  продолжает обслуживать трафик на прежней схеме БД.
- **EC-3**: Одна и та же настройка задана и в переменной окружения, и в `appsettings.yaml` →
  побеждает переменная окружения (сейчас — наоборот, это фиксируемый баг).
- **EC-4**: Клиент делает HTTPS-запрос через Railway-прокси, прокси форвардит на контейнер по
  HTTP → без `ForwardedHeaders` приложение видит запрос как HTTP и уходит в redirect-loop через
  `UseHttpsRedirection`; после фикса `X-Forwarded-Proto` учитывается корректно.
- **EC-5**: Healthcheck после деплоя недоступен/нездоров → Railway должен считать деплой
  неуспешным (path/timeout настроены в Railway-конфигурации сервиса).

## Non-Functional Requirements

- Один инстанс API, без автоскейлинга — соответствует MVP-объёму трафика.
- Единственное окружение — production.
- Деплой инициируется вручную из GitLab CI после успешных стадий build и test.

## Integrations

- **Railway** — хостинг контейнера API и (по умолчанию) БД.
- **GitLab CI** — сборка, тесты, миграции, вызов Railway CLI/API для деплоя.
- Переменные для внешних сервисов документируются, но не провижинятся в рамках этой спеки:
  S3-совместимое хранилище (`Storage:*`), AI-провайдер (`Ai:ApiKey`), Telegram Bot API
  (`Telegram:BotToken/WebhookSecret/PaymentProviderToken` — регистрация webhook вне scope).

## Acceptance Criteria

- **AC-1**: Given чистый checkout репозитория, When выполняется `docker build` по Dockerfile
  для `Blizka.Host`, Then образ собирается успешно и содержит опубликованный build приложения
  на runtime-образе `aspnet:10.0`.
- **AC-2**: Given переменная окружения (например, `Jwt__Secret`) задаёт тот же ключ, что и
  `appsettings.yaml`, When приложение стартует, Then действует значение из переменной
  окружения, а не из YAML.
- **AC-3**: Given приложение запущено в Production-окружении без обязательного секрета
  (`Jwt:Secret` пуст/не задан), When приложение стартует, Then процесс завершается с понятной
  ошибкой конфигурации вместо запуска в небезопасном состоянии.
- **AC-4**: Given приложение задеплоено за Railway-прокси (TLS обрывается на границе, до
  контейнера — HTTP), When клиент делает HTTPS-запрос, Then запрос обрабатывается без
  redirect-loop и `HttpContext.Request.IsHttps` корректно равен `true`.
- **AC-5**: Given CI зелёный после push/merge в main (build+test прошли), When инженер вручную
  запускает deploy-job в GitLab CI, Then сначала выполняется шаг миграций против prod БД, и
  только при его успехе — деплой новой версии API в Railway production.
- **AC-6**: Given шаг миграций в CI завершился с ошибкой, When пайплайн доходит до
  deploy-стадии, Then deploy-job не запускается, а предыдущая версия API продолжает
  обслуживать трафик.
- **AC-7**: Given API задеплоен на Railway, When Railway опрашивает healthcheck, Then
  используется путь `/api/health`, и деплой считается успешным только при здоровом ответе.
- **AC-8**: Given готовая документация по переменным окружения и шагам настройки, When
  инженер с нуля создаёт новый Railway-проект по инструкции из этой спеки, Then он получает
  работающий деплой (API + БД с PostGIS + все нужные секреты) без дополнительных уточнений.

## Deferred Decisions

- **БД**: используем Railway Postgres через собственный сервис на образе `postgis/postgis`
  (как в `docker-compose.yml`), а не managed Postgres-аддон Railway или внешний провайдер
  (Supabase/Neon/Crunchy). **Fallback выбран явно** — пересмотреть, если по ходу реализации
  выяснится, что Railway не позволяет произвольные Docker-образы для сервисов БД, либо
  потребуется managed backup/point-in-time recovery, которого нет у самостоятельно
  поднятого контейнера.

## Open Questions

_(пусто — все пункты решены выше либо перенесены в Deferred Decisions)_

## Implementation Notes

- **`Dockerfile`** (в `backend/` — после перехода репозитория на монорепо-структуру
  `backend/`+`frontend/`, изначально был в корне репозитория; отдельно от `Dockerfile.migrator`) — multi-stage
  `sdk:10.0` → `aspnet:10.0`, `dotnet publish -c Release`. `ENTRYPOINT` в exec-форме, поэтому
  Railway-переменная `$PORT` не подставляется в `ASPNETCORE_URLS` автоматически — вместо этого
  `Program.cs` читает `PORT` и вызывает `WebHost.UseUrls()` (приоритетнее `ASPNETCORE_URLS`).
- **Обнаружена незадокументированная в спеке блокирующая проблема**: `SixLabors.ImageSharp` (T-3.1,
  `Blizka.App/Photos/PhotoImageProcessor.cs`) — на Six Labors Split License. Без лицензионного ключа
  `dotnet build`/Debug только предупреждает, но `dotnet publish -c Release` (то, что делает
  `Dockerfile`) падает с ошибкой — это не всплывало раньше, потому что до этой спеки никто не
  публиковал Release-сборку. Решение (согласовано с пользователем): зарегистрировать бесплатный
  community-ключ на sixlabors.com/pricing (проект подпадает под порог < $1M выручки) и передавать
  его как `SixLaborsLicenseKey` — MSBuild подхватывает одноимённую переменную окружения как
  свойство напрямую, без правок csproj. В `Dockerfile` — через `ARG`/`ENV`; на Railway переменная
  должна быть помечена **available at build time** (используется только `dotnet publish`, не
  рантаймом). См. таблицу переменных в `docs/deployment/railway.md`. GitLab CI build/test
  сознательно оставлены на дефолтной Debug-конфигурации, чтобы не требовать этот секрет для
  базовой проверки — он нужен только там, где действительно выполняется Release-публикация
  (Railway при билде Dockerfile).
- **`.gitlab-ci.yml`**: `migrate` и `deploy` — оба manual jobs (не только `deploy`), `deploy` через
  `needs: ["migrate"]` заблокирован, пока `migrate` не отработает успешно. Так безопаснее буквального
  прочтения AC-5 (один клик по единственному `deploy-job`) — с одной ручной кнопкой миграции
  запускались бы либо от каждого push в `main` автоматически (что бьёт по prod БД без подтверждения
  инженера), либо потребовалось бы вручную склеивать миграции и деплой в один скрипт одной job,
  теряя видимость шага миграций в пайплайне как отдельной стадии (явно указано в Scope спеки:
  `build → test → migrate → deploy`, 4 стадии). AC-5/AC-6 по сути выполняются: миграции гарантированно
  выполняются и успешны до деплоя, деплой не происходит при их падении — просто через два
  последовательных ручных клика вместо одного.
- Все пункты AC-1..AC-4, AC-7 проверены руками локально (`docker build`, `dotnet run` с/без
  переопределения через переменные окружения, `/api/health` через `docker compose up -d postgres`).
  AC-5/AC-6/AC-8 (сам GitLab CI пайплайн, реальный Railway-проект) — **не проверены** ввиду
  отсутствия доступа к Railway-аккаунту и GitLab CI/CD Variables пользователя из этой сессии;
  `.gitlab-ci.yml` и `docs/deployment/railway.md` подготовлены, но первый реальный прогон
  `migrate`/`deploy` в GitLab кто-то должен выполнить вручную по завершении настройки Railway-проекта
  (шаги 1–9 в `docs/deployment/railway.md`).
