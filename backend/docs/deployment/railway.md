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
   под `/var/lib/postgresql/data`, иначе данные теряются при пересоздании контейнера. Дополнительно
   задать `PGDATA=/var/lib/postgresql/data/pgdata` — точка монтирования тома на Railway содержит
   служебную директорию `lost+found` (артефакт файловой системы), и `initdb` отказывается
   инициализировать кластер прямо в непустую точку монтирования; `PGDATA` заставляет писать данные
   в чистую поддиректорию внутри того же тома.
3. **Сервис хранилища фото (MinIO)**: добавить сервис из **Docker Image**, образ
   `minio/minio:RELEASE.2025-09-07T16-13-09Z-cpuv1` (тот же, что в `docker-compose.yml`), Custom
   Start Command `minio server /data --console-address ":9001"` — в отличие от `command:` в
   `docker-compose.yml` (который подменяет только `CMD`, оставляя `ENTRYPOINT`-скрипт образа,
   сам добавляющий `minio` перед `server`), Custom Start Command в Railway заменяет entrypoint
   целиком, поэтому бинарник `minio` нужно указывать явно. Переменные `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` —
   это и есть будущие `Storage__AccessKey` / `Storage__SecretKey` API-сервиса. Подключить volume под
   `/data`, иначе фото теряются при пересоздании контейнера. В Settings → Networking сгенерировать
   публичный домен на порт **9000** (S3 API — фото отдаются клиенту напрямую по этому домену, см.
   `S3PhotoStorageService.UploadAsync`, который возвращает `PublicBaseUrl + key`); порт 9001
   (консоль) публично не открывать. После первого деплоя один раз вручную создать бакет и включить
   анонимное скачивание — аналог одноразового сервиса `minio-init` из `docker-compose.yml`, но там
   он в compose пересоздаётся при каждом `up`, а тут это разовая ручная операция:
   ```bash
   mc alias set railway-minio https://<публичный-домен-minio> <MINIO_ROOT_USER> <MINIO_ROOT_PASSWORD>
   mc mb --ignore-existing railway-minio/blizka-photos
   mc anonymous set download railway-minio/blizka-photos
   ```
4. **Сервис API**: Railway не имеет нативной интеграции с GitLab (только с GitHub), поэтому
   вариант «Deploy from repo» в UI для этого репозитория недоступен — добавить сервис как
   **Empty Service** (New → Empty Service), без привязки к какому-либо репозиторию. Деплой
   выполняется исключительно через `railway up` из `.gitlab-ci.yml` (CLI-push, см. ниже), а не
   через git-интеграцию Railway. В настройках сервиса: билдер — **Dockerfile**, Dockerfile Path
   `Dockerfile`. Root Directory задавать не нужно — `deploy`-джоба в `.gitlab-ci.yml` делает `cd
   backend` перед `railway up`, так что в Railway загружается уже папка `backend/` как корень
   контекста сборки.
5. **Healthcheck**: в настройках сервиса API задать Healthcheck Path `/api/health`
   (см. AC-7 спеки — Railway считает деплой успешным только при здоровом ответе на этот путь).
6. **Переменные окружения сервиса API** — см. таблицу ниже. Задаются в Railway UI (Variables) в
   формате `Section__Key` (двойное подчёркивание — стандартный для .NET способ адресовать вложенные
   секции конфигурации через переменные окружения).
7. **`SixLaborsLicenseKey`** — единственная переменная, которая должна быть помечена в Railway как
   доступная **во время сборки** (Build-time / "Available at build time"), а не только в рантайме:
   она используется `dotnet publish -c Release` внутри `Dockerfile`, а не самим запущенным
   приложением. Остальные переменные ниже — только рантайм.
8. **Railway API-токен для CI**: Railway → Project Settings → Tokens → создать Project Token (или
   Service Token, если хочется ограничить деплой одним сервисом). Добавить его в GitLab
   (Settings → CI/CD → Variables) как `RAILWAY_TOKEN`, отметить **Protected** и **Masked**.
9. **Имя сервиса для CI**: добавить переменную `RAILWAY_SERVICE` в GitLab CI/CD Variables со
   значением имени API-сервиса в Railway (как оно называется в самом проекте Railway) — используется
   в `railway up --service "$RAILWAY_SERVICE"`.
10. **Строка подключения к prod БД для шага миграций**: добавить `BLIZKA_DB_CONNECTION_PROD` в
   GitLab CI/CD Variables (Protected + Masked) — формат такой же, как у `BLIZKA_DB_CONNECTION` в
   `docker-compose.yml`. Хост/порт **не** брать из сгенерированного HTTP-домена сервиса
   (Settings → Networking → Public Networking) — тот проксирует только HTTP(S) на 443/80 и не
   годится для сырого TCP-протокола Postgres (даёт `Timeout during connection attempt` на 5432,
   т.к. порт там просто ничего не слушает). Нужен отдельный **TCP Proxy** (та же вкладка
   Networking, отдельная секция) с target port `5432` — Railway выдаст собственные `Proxy
   Domain`/`Proxy Port` (порт нестандартный, не 5432), их и использовать: `Host=<Proxy
   Domain>;Port=<Proxy Port>;Database=blizka;Username=blizka;Password=...`. (Это для сырого Docker
   Image сервиса на `postgis/postgis` — у managed Postgres-аддона Railway были бы готовые
   `PGHOST`/`PGPORT`/`DATABASE_PUBLIC_URL`, но мы его сознательно не используем, см. Deferred
   Decisions в спеке.)

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
| `Storage__Provider` | нет (дефолт `S3`) | Остаётся `S3` — MinIO реализует тот же API, отдельного провайдера в коде для него нет. |
| `Storage__Endpoint` | да | Internal-адрес MinIO-сервиса из п.3: `http://<имя-minio-сервиса>.railway.internal:9000` — трафик загрузки/удаления фото идёт по приватной сети Railway, не через публичный домен. **Без пути бакета** (не `.../blizka-photos`) — `ForcePathStyle=true` (T-3.1) сам подставляет `/{Bucket}` к этому адресу при каждом запросе; если бакет уже есть в `Endpoint`, объекты физически ложатся по задвоенному пути (`bucket=blizka-photos`, `key=blizka-photos/photos/...`) и все публичные ссылки на фото превращаются в 404 — без явной ошибки при загрузке, воспроизведено на проде 2026-08-24. Хост теперь падает при старте, если `Endpoint` оканчивается на `/{Bucket}` (fail-fast guard, `DataServiceCollectionExtensions`), но переменную всё равно нужно задать правильно с самого начала. |
| `Storage__Bucket` | да | `blizka-photos` (бакет создаётся вручную в п.3). |
| `Storage__AccessKey` / `Storage__SecretKey` | да | `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` MinIO-сервиса из п.3. |
| `Storage__PublicBaseUrl` | да | `https://<публичный-домен-minio>/blizka-photos` — публичный домен из п.3 (порт 9000), по нему клиент грузит фото напрямую. |
| `Ai__Provider` | нет (дефолт `OpenAI`) | — |
| `Ai__ApiKey` | да | Ключ AI-провайдера (провижининг — вне scope). |
| `Ai__Model` | нет (дефолт `gpt-4o-mini`) | — |
| `Cors__AllowedOrigins__0` | да | `https://web.telegram.org` (и любые дополнительные origin'ы — `__1`, `__2`, ...). |
| `SixLaborsLicenseKey` | да, **build-time** | Community-лицензия для `SixLabors.ImageSharp` (используется в T-3.1, обработка фото) — без неё `dotnet publish -c Release` в `Dockerfile` падает с ошибкой (в Debug-сборке это только предупреждение, поэтому раньше это не всплывало). Зарегистрировать бесплатный ключ на https://sixlabors.com/pricing/, если организация подпадает под порог Six Labors Split License (< $1M годовой выручки) — иначе нужна платная лицензия. |
| `ASPNETCORE_ENVIRONMENT` | нет (дефолт `Production`, задан в `Dockerfile`) | Переопределять не нужно. |
| `DevLogin__Secret` | нет (дефолт — пусто, фичи выключены) | Спека 003 (docs/specs/003-demo-seed-data.md): включает `POST /api/dev/reseed-demo-data` и dev-логин в обход Telegram (`X-Dev-Login-Secret`/`X-Dev-Login-TelegramId` на `POST /api/auth/telegram`, только для 10 фиксированных демо-пользователей). Задавать вручную только на время, пока фронтендеру нужен доступ к демо-данным. |

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
- Получение AI API-ключа — только переменная задокументирована выше (провижининг MinIO как
  S3-совместимого хранилища, в отличие от стороннего S3-провайдера, описан в п.3 выше).
- Автоматический (push-triggered) continuous deployment.
