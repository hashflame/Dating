# Деплой Blizka.Host на Railway

Реализация спеки [`docs/specs/001-railway-deployment.md`](../specs/001-railway-deployment.md).
Здесь — переменные окружения и пошаговая инструкция первоначальной настройки; сам механизм
(`Dockerfile`, `.gitlab-ci.yml`, фиксы в `Program.cs`) описан в спеке.

## Первоначальная настройка Railway-проекта

1. **Создать проект** в Railway (railway.app) — один проект на приложение, без staging
   (см. Scope спеки: одно окружение — `production`).
2. **Сервис БД**: добавить сервис из **Docker Image**, образ `postgis/postgis:16-3.4` (тот же,
   что в `docker-compose.yml`) — не managed Postgres-аддон Railway (см. Deferred Decisions в
   спеке). Задать переменные `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`; подключить volume
   под `/var/lib/postgresql/data`, иначе данные теряются при пересоздании контейнера.
3. **Сервис API**: добавить сервис из **этого репозитория** (GitHub/GitLab-интеграция Railway,
   либо ручной деплой через CLI — см. ниже), билдер — **Dockerfile**. Репозиторий — монорепа
   (`backend/` + `frontend/`), поэтому Railway не обнаружит `Dockerfile` автоматически — указать
   явно в настройках сервиса: Root Directory `/backend`, Dockerfile Path `Dockerfile` (путь
   относительно Root Directory).
4. **Healthcheck**: в настройках сервиса API задать Healthcheck Path `/api/health`
   (см. AC-7 спеки — Railway считает деплой успешным только при здоровом ответе на этот путь).
5. **Переменные окружения сервиса API** — см. таблицу ниже. Задаются в Railway UI (Variables) в
   формате `Section__Key` (двойное подчёркивание — стандартный для .NET способ адресовать вложенные
   секции конфигурации через переменные окружения).
6. **`SixLaborsLicenseKey`** — единственная переменная, которая должна быть помечена в Railway как
   доступная **во время сборки** (Build-time / "Available at build time"), а не только в рантайме:
   она используется `dotnet publish -c Release` внутри `Dockerfile`, а не самим запущенным
   приложением. Остальные переменные ниже — только рантайм.
7. **Railway API-токен для CI**: Railway → Project Settings → Tokens → создать Project Token (или
   Service Token, если хочется ограничить деплой одним сервисом). Добавить его в GitLab
   (Settings → CI/CD → Variables) как `RAILWAY_TOKEN`, отметить **Protected** и **Masked**.
8. **Имя сервиса для CI**: добавить переменную `RAILWAY_SERVICE` в GitLab CI/CD Variables со
   значением имени API-сервиса в Railway (как оно называется в самом проекте Railway) — используется
   в `railway up --service "$RAILWAY_SERVICE"`.
9. **Строка подключения к prod БД для шага миграций**: добавить `BLIZKA_DB_CONNECTION_PROD` в
   GitLab CI/CD Variables (Protected + Masked) — формат такой же, как у `BLIZKA_DB_CONNECTION` в
   `docker-compose.yml`, но с адресом/паролем прод-БД Railway (Railway показывает его в переменных
   сервиса БД как `DATABASE_PUBLIC_URL`/отдельные `PGHOST`/`PGPORT`/`PGUSER`/`PGPASSWORD`/`PGDATABASE`
   — собрать из них `Host=...;Port=...;Database=...;Username=...;Password=...`).

## Переменные окружения сервиса API (Railway)

Те же секции, что уже объявлены в `appsettings.yaml` — Railway задаёт их как обычные Service
Variables (рантайм), кроме `SixLaborsLicenseKey` (build-time, см. пункт 6 выше).

| Переменная | Обязательна | Назначение |
|---|---|---|
| `Database__ConnectionString` | да | Строка подключения к prod Postgres (сервис БД в том же Railway-проекте). |
| `Jwt__Secret` | да | Ключ подписи JWT. Пустое значение — хост не стартует (fail-fast валидация, см. T-1.1). Сгенерировать случайную строку ≥32 байт, никогда не переиспользовать dev-значение из `appsettings.Development.yaml`. |
| `Jwt__Issuer` | нет (есть дефолт `blizka`) | Обычно не переопределяется. |
| `Jwt__Audience` | нет (есть дефолт `blizka-clients`) | Обычно не переопределяется. |
| `Jwt__TtlHours` | нет (есть дефолт `24`) | Время жизни токена. |
| `Telegram__BotToken` | да | Токен Telegram-бота — без него не работает валидация `initData` (T-1.1) и Bot API сервис (T-10.1). |
| `Telegram__WebhookSecret` | да, если используется webhook | Секрет для проверки, что запрос на webhook пришёл от Telegram. Регистрация самого webhook — **вне scope этой спеки**, отдельная ручная задача. |
| `Telegram__PaymentProviderToken` | да, если включены платежи звёздами | Токен провайдера платежей Telegram. |
| `Storage__Provider` | нет (дефолт `S3`) | — |
| `Storage__Endpoint` | да | Endpoint S3-совместимого хранилища (провижининг — вне scope, см. спеку). |
| `Storage__Bucket` | да | Имя бакета для фото. |
| `Storage__AccessKey` / `Storage__SecretKey` | да | Ключи доступа к хранилищу. |
| `Storage__PublicBaseUrl` | да | Публичный базовый URL, из которого клиент грузит фото напрямую (см. `docker-compose.yml` — тот же принцип, что и для MinIO локально). |
| `Ai__Provider` | нет (дефолт `OpenAI`) | — |
| `Ai__ApiKey` | да | Ключ AI-провайдера (провижининг — вне scope). |
| `Ai__Model` | нет (дефолт `gpt-4o-mini`) | — |
| `Cors__AllowedOrigins__0` | да | `https://web.telegram.org` (и любые дополнительные origin'ы — `__1`, `__2`, ...). |
| `SixLaborsLicenseKey` | да, **build-time** | Community-лицензия для `SixLabors.ImageSharp` (используется в T-3.1, обработка фото) — без неё `dotnet publish -c Release` в `Dockerfile` падает с ошибкой (в Debug-сборке это только предупреждение, поэтому раньше это не всплывало). Зарегистрировать бесплатный ключ на https://sixlabors.com/pricing/, если организация подпадает под порог Six Labors Split License (< $1M годовой выручки) — иначе нужна платная лицензия. |
| `ASPNETCORE_ENVIRONMENT` | нет (дефолт `Production`, задан в `Dockerfile`) | Переопределять не нужно. |

Переменные окружения имеют приоритет над `appsettings.yaml`/`appsettings.{Environment}.yaml`
(см. фикс порядка источников конфигурации в спеке, EC-3) — значения из таблицы всегда побеждают.

## GitLab CI/CD Variables

Помимо `RAILWAY_TOKEN`, `RAILWAY_SERVICE` и `BLIZKA_DB_CONNECTION_PROD` (см. пункты 7–9 выше):

| Переменная | Обязательна | Назначение |
|---|---|---|
| `RAILWAY_PUBLIC_URL` | нет | Публичный URL API — используется только как `environment.url` в GitLab (декоративно, для кнопки "View deployment" в UI). |

## Пайплайн (`.gitlab-ci.yml`)

`build → test → migrate → deploy`. `migrate` и `deploy` — manual jobs, доступны только на `main`.
`deploy` использует `needs: ["migrate"]` — GitLab держит `deploy` в состоянии "заблокирован", пока
`migrate` не запущен и не завершился успешно (это соответствует AC-5/AC-6 спеки: деплой не
происходит, пока миграции не прошли).

Порядок ручного запуска:
1. Смержить/запушить в `main` → `build` и `test` проходят автоматически.
2. Вручную запустить `migrate` — применяет миграции к prod БД через `Dockerfile.migrator`.
3. После успеха `migrate` вручную запустить `deploy` — `railway up --service "$RAILWAY_SERVICE"`.

Если `migrate` падает — `deploy` остаётся заблокированным, предыдущая версия API продолжает
обслуживать трафик на прежней схеме БД (AC-6).

## Что не входит в эту итерацию

- Регистрация Telegram webhook (`setWebhook` на Railway-домен) — ручная задача отдельно.
- Staging-окружение.
- Провижининг S3-бакета и получение AI API-ключа — только переменные задокументированы выше.
- Автоматический (push-triggered) continuous deployment.
