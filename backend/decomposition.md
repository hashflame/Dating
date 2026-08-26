# Блізка — Декомпозиция задач Backend

> **Контекст:** .NET 10 · Web API · PostgreSQL · Telegram Mini App  
> **Источник:** спецификация интерфейса v1.0 (30 экранов) + backend-spec  
> **Дата:** 2026-08-18

---

## Принципы декомпозиции

Каждая задача — изолированный блок, который можно отдать в отдельный чат (Claude Code, ручная разработка, код-ревью). Задачи пронумерованы внутри эпика и содержат:

- **Входные данные** — что нужно знать перед началом работы.
- **Результат** — что должно быть готово на выходе.
- **Зависимости** — какие задачи должны быть завершены до начала.
- **Экраны** — привязка к макетам (S-xx).

### MVP vs Post-MVP

Критерий MVP — минимальный путь пользователя от регистрации до реальной встречи:

1. Зарегистрироваться (онбординг).
2. Увидеть ленту и свайпать.
3. Получить мэтч.
4. Написать человеку в Telegram (открыть контакт за зорку).

Всё, что ускоряет этот путь или без чего путь невозможен — MVP. Остальное — Post-MVP, помечено тегом `[POST-MVP]`.

---

## Эпик 0 · Фундамент проекта

### T-0.1 · Инициализация проекта и структура решения `[MVP]`

**Результат:** Solution с проектами, базовая конфигурация, CI-ready. ✅ Реализовано.

**Что сделано:**
- Solution `Blizka.sln` со структурой проектов (4 слоя вместо изначальных 6 — `Domain`/`Application` объединены в `App`, `Contracts` вошёл в `Api`):
  - `Blizka.Api` — класс-библиотека: контроллеры, DTO запросов/ответов. Не entry point — подключается к `Host` через `AddApplicationPart`.
  - `Blizka.App` — доменные сущности, enums, интерфейсы, use cases (MediatR), валидация (FluentValidation). Ядро, ни от кого не зависит.
  - `Blizka.Data` — EF Core (Npgsql + PostGIS), репозитории, внешние сервисы.
  - `Blizka.Host` — entry point (`Microsoft.NET.Sdk.Web`), composition root: DI-регистрация слоёв, Serilog, CORS, Quartz hosting.
  - `Blizka.UnitTests` — unit-тесты (`App` + `Data`, без хоста).
  - `Blizka.IntegrationTests` — integration-тесты через `WebApplicationFactory<Program>` (ссылается на `Host`).
- `appsettings.yaml` / `appsettings.Development.yaml` (YAML вместо JSON, через `NetEscapades.Configuration.Yaml`) с секциями: `Database`, `Telegram`, `Storage`, `Ai`, `Cors`, `Serilog`, `Logging`. Секции `Redis` нет — сознательно отложен, добавим вместе с первой задачей, которая его реально потребует (кандидат — кэш ленты в T-5.1).
- NuGet: EF Core + Npgsql + `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite`, FluentValidation, Serilog (`Serilog.AspNetCore`), **Quartz** (не Hangfire — выбор зафиксирован), MediatR. Версии зафиксированы через Central Package Management (`Directory.Packages.props`).
- Базовый global error handling: `AddProblemDetails()` + `UseExceptionHandler()` — заглушка на замену в T-0.3 (единый `ApiResponse<T>`/`ApiError`).
- CORS-политика `TelegramMiniApp` — origin(ы) из `Cors:AllowedOrigins`.
- `docker-compose.yml`: только PostgreSQL 16 + PostGIS (`postgis/postgis:16-3.4`), без Redis.
- `GET /api/health` — health-check эндпоинт (не было в исходном требовании, добавлено для Docker/CI); реализован через `Microsoft.Extensions.Diagnostics.HealthChecks` + `AspNetCore.HealthChecks.NpgSql` (проверяет реальное соединение с Postgres, не просто liveness-заглушка), JSON-формат кастомный (`Blizka.Api.Common.HealthCheckResponseWriter`), регистрация чек-апа — в `AddDataLayer` рядом с `AddDbContext`.
- Scalar UI (`Scalar.AspNetCore`) поверх уже подключённого `AddOpenApi()` — `/scalar/v1`, только в Development, рядом с `MapOpenApi()`.
- `CLAUDE.md` в корне — гайд для работы с репозиторием (команды, архитектура, договорённости), поддерживается в актуальном состоянии по ходу разработки.
- **Дополнение (после фидбека фронтенда, 2026-08-25): числовые поля в OpenAPI-спеке выходили как `type: ["integer","string"]`.** `AddControllers()` строит `JsonSerializerOptions` через `JsonSerializerDefaults.Web`, который включает `NumberHandling.AllowReadingFromString` (числа принимаются и из JSON-строк) — удобно для веб-форм, но `System.Text.Json.JsonSchemaExporter` (на нём построен `AddOpenApi()`) в ответ честно описывает числовое поле как объединение `integer`/`string` с числовым `pattern`, что ломает строгую валидацию у клиентов, сгенерированных по спеке. Возвращено `NumberHandling.Strict` — **в двух местах**: `ApiServiceCollectionExtensions.AddApiLayer` (`Microsoft.AspNetCore.Mvc.JsonOptions`, реальный рантайм-парсинг тела запроса) и `Program.cs` (`Microsoft.AspNetCore.Http.Json.JsonOptions` — отдельный набор опций, которым пользуется именно генератор OpenAPI-схемы, не MVC). Без второго места рантайм стал бы строгим, а спека — нет.

**Зависимости:** нет.

---

### T-0.2 · Доменные сущности и EF Core конфигурация `[MVP]`

**Результат:** Все entity classes + EF configurations + миграции, БД создаётся и seed-ится. ✅ Реализовано.

**Важно:** файла `backend-spec.md` (раздел 25) в репозитории нет — есть только этот `decomposition.md`. Формы сущностей ниже собраны по крупицам из упоминаний в других разделах (T-1.1, T-2.x, T-5.x, T-7.x, T-8.1, T-9.x, T-11.1, T-14.1, T-17.1, T-19.1, T-20.1 и т.д.), а не скопированы из авторитетного списка полей. Значения enum-ов без явного якоря в тексте (`DatingGoal`, `Smoking`, `Drinking`, `Chronotype`), категории интересов и точные координаты городов — решения по умолчанию; задачи, которым реально принадлежит фича (T-4.1 для полного гео-справочника, T-9.3 для предпочтений на свидания, T-14.1 для каталога дилемм), могут их уточнить без новой фундаментальной миграции.

**Уточнение по `DatingGoal` (после сверки с макетом фронтенда):** добавлены `FamilyAndKids`, `HobbyCompany`, `Chatting` — в макете шага «Цель знакомства» этих целей не было в T-0.2 при заведении дефолтного набора значений. `NotSureYet` в коде оставлен (в макете такого варианта нет, но ничего в бэкенде на него не завязано жёстко) — фронтенду достаточно просто не показывать его в UI. Миграция не нужна: колонка `text`/`text[]` через `HasConversion<string>()`, схема не меняется.

**Что сделано:**
- 20 entity classes в `Blizka.App/Domain/Entities` (POCO, без ссылок на EF Core/ASP.NET Core) + 17 enum-ов в `Blizka.App/Domain/Enums`, все — `.HasConversion<string>()`:
  - MVP: `User`, `Photo`, `Interest`, `UserInterest`, `City`, `Swipe`, `Match`, `SparkTransaction`.
  - Post-MVP (тоже перечислены в этой задаче, реализованы сразу, чтобы не заводить отдельную миграцию под каждую фичу): `QuestionOfDay`, `QuestionAnswer`, `Minigame`, `MinigameAnswer`, `Idea`, `IdeaVote`, `DatePreference`, `UserDatePreference`, `Report`, `TelegramPayment`, `Subscription`, `CityWaitlist`.
  - Сущности других MVP-задач (`OnboardingDraft`, `UserConsent`, `UserFilter`, `PrivacySettings`, `UserBlock`, `Referral`, `Notification`) сознательно не созданы — не входят в список этой задачи, будут добавлены вместе с T-2.1/T-2.2/T-5.4/T-16.1/T-16.2/T-20.1.
- `Blizka.App` получил прямую зависимость на `NetTopologySuite` (не EF/ASP.NET — обычная geometry-библиотека) — `User.Coordinates`/`City.Coordinates` типизированы как `Point`/`Point?`.
- 20 `IEntityTypeConfiguration<T>` в `Blizka.Data/Configurations`:
  - `User.TelegramId` (unique), `Swipe(FromUserId, ToUserId)` (unique), `Match(User1Id, User2Id)` (unique, порядок канонизируется в коде — меньший Guid как `User1Id`, Postgres не умеет unique для неупорядоченной пары напрямую).
  - `City.Name` — по факту 3 колонки локали (`NameRu`/`NameBe`/`NameEn`), под каждую отдельный GIN trigram-индекс (`gin_trgm_ops`), т.к. поиск (T-4.1) принимает `locale`.
  - PostGIS: `Coordinates` → `.HasColumnType("geography (Point, 4326)")` на `User` и `City`.
  - Delete behavior: дети одного пользователя (`Photo`, `UserInterest`, `SparkTransaction`, `UserDatePreference`) — cascade; связи между двумя пользователями (`Swipe`, три FK на `Match`, `QuestionAnswer`, `MinigameAnswer`, `Report`) — restrict (у `User` soft-delete через `Status`/`DeletedAt`, а не hard delete).
- Seed через `HasData` (детерминированные литеральные GUID вида `00000000-0000-0000-0aXX-...`):
  - Интересы: 5 категорий × 8 = 40 (Спорт и активность, Творчество и искусство, Развлечения и отдых, Еда и напитки, Саморазвитие и путешествия) — состав категорий не из спеки, легко переименовать позже.
  - Города: 28 крупнейших городов Беларуси с приблизительными координатами, `Country = "BY"`, `IsOpen = true` — стартовый каталог, полный гео-справочник (+ диаспора PL/LT/LV/RU/UA) заводит T-4.1.
  - `DatePreference`: 4 фиксированных значения из T-9.3.
  - Каталог дилемм для `Minigame` не сеется — принадлежит T-14.1, `Minigame.DilemmaIds` пока просто `int[]`-плейсхолдер.
- `BlizkaDbContext` получил 20 `DbSet<T>`.
- Миграция `InitialCreate` сгенерирована (`dotnet ef migrations add`) и проверена чтением сгенерированного SQL — типы колонок, unique/GIN-индексы, FK/cascade и `InsertData` для сидов выглядят корректно. **Не проверена на живой БД** — Docker Desktop в момент реализации не был запущен, `docker compose up -d postgres` не выполнялся.

**Зависимости:** T-0.1.

---

### T-0.3 · Формат ответов API, пагинация, ошибки `[MVP]`

**Результат:** Единообразный формат всех ответов, middleware обработки ошибок. ✅ Реализовано.

**Важно:** раздела 26.3 spec (на который ссылается формулировка задачи) в репозитории нет — состав полей `ApiError` и конкретные HTTP-коды/`action` для каждого исключения ниже собраны по смыслу задачи, а не скопированы из авторитетного списка. При появлении реального backend-spec их стоит сверить.

**Что сделано:**
- `Blizka.App/Domain/Exceptions`: `BlizkaDomainException` — базовый класс (не зависит от ASP.NET Core) с `ErrorCode` (строка) и `Details` (`IReadOnlyDictionary<string, object?>?`) для структурированного контекста. От него унаследованы 4 кастомных исключения из задачи: `InsufficientSparksException(required, available)`, `UserBannedException(userId)`, `OnboardingIncompleteException(missingStep?)`, `CityNotOpenException(cityId)`. Локализованный текст исключения не хранят — только `ErrorCode`; сообщение для пользователя резолвится в `Blizka.Api`.
- `Blizka.Api/Common`: `ApiResponse<T>` (обёртка `{ data }` для успешных ответов), `ApiError`/`ApiErrorResponse` (обёртка `{ error: { code, message, details, action } }`), `PaginatedResponse<T>` (`items`, `page`, `pageSize`, `totalCount`, `hasMore` — `hasMore` вычисляемое свойство, не хранится отдельно).
- `Blizka.Api/ErrorHandling`: `BlizkaExceptionHandler` — `IExceptionHandler` (не classic middleware — актуальный для .NET 8+/10 механизм, всё ещё вешается через `UseExceptionHandler()`), маппит exception → HTTP-статус → `ApiErrorResponse`. Коды/статусы/action, которых нет в тексте задачи и являются решением по умолчанию: `InsufficientSparksException` → 402 + `TOP_UP_SPARKS`, `UserBannedException` → 403 + `CONTACT_SUPPORT`, `OnboardingIncompleteException` → 422 + `COMPLETE_ONBOARDING` (422 выбран по аналогии с T-2.2, где отсутствие согласия тоже даёт 422), `CityNotOpenException` → 409 + `JOIN_CITY_WAITLIST`, `FluentValidation.ValidationException` → 400 + `VALIDATION_ERROR` (details — словарь `field → messages[]`), всё остальное → 500 + `INTERNAL_ERROR` (сообщение исключения в ответ не попадает, только в лог).
- Локализация — не `IStringLocalizer`/`.resx`, а простой `ErrorMessageCatalog` (словарь `ErrorCode → { ru, be, en }` текстов, каждый явно объясняет действие, не просто описывает ошибку). Язык резолвится в `BlizkaExceptionHandler.ResolveLocale`: сначала claim `locale` у `HttpContext.User` (JWT из T-1.1 — этой задачи ещё нет, так что на практике claim'а пока не будет), иначе заголовок `Accept-Language` (сравнение по primary subtag, `en-US` тоже матчится на `en`), иначе `ru` по умолчанию.
- `Program.cs`: `AddProblemDetails()` не убран, а оставлен рядом с новым `AddExceptionHandler<BlizkaExceptionHandler>()` — без него `UseExceptionHandler()` кидает `InvalidOperationException` при старте хоста (ASP.NET Core требует либо `ExceptionHandler`/`ExceptionHandlingPath` в опциях, либо зарегистрированный `ProblemDetailsService`, даже если свой `IExceptionHandler` уже покрывает все случаи и всегда возвращает `true`). Проверено запуском реального хоста — без `AddProblemDetails()` приложение падало на старте.
- Тесты: `Blizka.UnitTests/Domain/Exceptions` — конструирование исключений и их `ErrorCode`/`Details`. `Blizka.IntegrationTests/ErrorHandling/BlizkaExceptionHandlerTests` — поднимает отдельный минимальный `TestServer` (не `WebApplicationFactory<Program>`, чтобы не тянуть CORS/Telegram-конфиг Host'а) с реальным `BlizkaExceptionHandler`, проверяет статус-коды, `action`, `details`, локализацию по claim'у/заголовку/фолбэку. `Blizka.IntegrationTests/Common/PaginatedResponseTests` — `HasMore` на граничных значениях.

**Зависимости:** T-0.1.

---

## Эпик 1 · Аутентификация

### T-1.1 · Telegram initData валидация middleware `[MVP]`

**Экраны:** S-01.

**Результат:** Middleware, который валидирует каждый запрос и выдаёт JWT. ✅ Реализовано.

**Что сделать:**
- `TelegramAuthMiddleware` — извлекает `initData` из заголовка `X-Telegram-InitData`.
- Парсинг query string: `query_id`, `user` (JSON), `auth_date`, `hash`.
- HMAC-SHA256 валидация: `secret = HMAC_SHA256("WebAppData", bot_token)`, `hash = HMAC_SHA256(secret, data_check_string)`.
- Проверка `auth_date` не старше 5 минут.
- Endpoint `POST /api/auth/telegram`:
  - Если пользователь не существует — создать `User` со статусом `New`, подтянуть имя + аватар из `initData.user`.
  - Если существует — обновить `LastActiveAt`.
  - Вернуть JWT (TTL 24ч) с claims: `userId`, `telegramId`, `locale`, `status`.
- Проверка статуса: `Banned` → 403, `Deleted` → 410.

**Что сделано:**
- `Blizka.App\Telegram\TelegramInitDataValidator` — чистый статический класс (без ASP.NET/EF): парсит query-string `initData`, считает `secret = HMAC_SHA256("WebAppData", botToken)` и `hash = HMAC_SHA256(secret, data_check_string)` (поля отсортированы по ключу, `\n`-разделитель, без `hash`), сравнивает constant-time (`CryptographicOperations.FixedTimeEquals`), проверяет `auth_date` (не старше 5 минут от текущего времени) и парсит `user` JSON в `TelegramInitData`. Полностью покрыт unit-тестами (`TelegramInitDataValidatorTests`) без HTTP-инфраструктуры.
- `Blizka.Api\Auth\TelegramAuthMiddleware` — реальный ASP.NET Core middleware, но активируется только на `POST /api/auth/telegram` (остальные роуты проходят насквозь — у них уже есть JWT, а не сырой initData). При успехе кладёт распарсенный `TelegramInitData` в `HttpContext.Items`; при неудаче — 401 с локализованным `ApiErrorResponse` (код `TELEGRAM_INIT_DATA_INVALID`, добавлен в `ErrorMessageCatalog`), причина неуспеха логируется на `Warning`, но не уходит клиенту.
- `AuthController.Telegram` (`POST /api/auth/telegram`, `[AllowAnonymous]`) читает `TelegramInitData` из `HttpContext.Items` и шлёт `AuthenticateTelegramUserCommand` через MediatR.
- `AuthenticateTelegramUserCommandHandler` (`Blizka.App\UseCases\Auth`) — первый MediatR use-case в проекте с доступом к БД: заводит `IUserRepository` (интерфейс в `Blizka.App\Domain\Repositories`, реализация `UserRepository` в `Blizka.Data\Repositories`, EF-based) как паттерн для будущих задач. Создаёт `User` со статусом `New` (имя — `firstName [+ " " + lastName]`, locale — из `language_code`, если это `ru`/`be`/`en`, иначе `ru`) либо обновляет `LastActiveAt` у существующего. Проверка статуса (`Banned` → `UserBannedException`/403, `Deleted` → новый `UserDeletedException`/410) — после создания/обновления в памяти, но до `SaveChangesAsync`, так что забаненный/удалённый пользователь не получает никаких побочных изменений в БД.
- **Аватар из `initData.user.photo_url` сознательно не импортируется в этой задаче** — скачивание файла и заливка в S3-хранилище принадлежат `POST /api/users/me/photos/import-telegram` из T-3.1; здесь только сохраняются `id`/`first_name`/`last_name`/`username`/`photo_url`/`language_code` как распарсенные данные (сам `photo_url` пока никуда не пишется).
- JWT: `Blizka.App\Auth\JwtTokenService` (+`IJwtTokenService`, `JwtOptions`) — выдаёт HS256-токен с claims `userId`/`telegramId`/`locale`/`status`, TTL из конфига (`Jwt:TtlHours`, дефолт 24ч). Использует `System.IdentityModel.Tokens.Jwt`/`Microsoft.IdentityModel.Tokens` напрямую в `Blizka.App` — по той же логике, что и `NetTopologySuite` там же (CLAUDE.md): это чистые библиотеки токенов, не ASP.NET Core/EF Core.
- `ApiServiceCollectionExtensions.AddApiLayer` (сигнатура расширена, теперь принимает `IConfiguration`) регистрирует `AddAuthentication().AddJwtBearer(...)` с валидацией issuer/audience/lifetime/signing-key из секции `Jwt`, плюс `AddAuthorization()` — так что claims из выданного JWT сразу доступны будущим `[Authorize]`-эндпоинтам через `HttpContext.User`. `Program.cs` вызывает `app.UseMiddleware<TelegramAuthMiddleware>()` перед `UseAuthentication()`/`UseAuthorization()`.
- Новая конфиг-секция `Jwt` (`Secret`/`Issuer`/`Audience`/`TtlHours`) в `appsettings.yaml` — `Secret` пустой, как и у остальных секретов (`Telegram:BotToken` и т.п.). В `appsettings.Development.yaml` — dev-only сгенерированный ключ (явно помечен комментарием, что это не для прод), иначе `dotnet run` в Development локально не поднимется (пустой ключ ломает `SymmetricSecurityKey`).
- **Fail-fast валидация `Jwt:Secret` при старте хоста.** Изначально пустой `Secret` не проверялся нигде: `app.UseAuthentication()` вызывается на каждом запросе (до `MapControllers`), и построение `SymmetricSecurityKey` с пустым ключом бросает `ArgumentException` только при первом реальном HTTP-запросе — т.е. в любом окружении без переопределённого секрета (staging/prod) приложение отвечало бы 500 на буквально любой запрос с непонятной крипто-ошибкой. Исправлено через `services.AddOptions<JwtOptions>().Bind(...).Validate(...).ValidateOnStart()` в `AddApiLayer` — теперь при отсутствии секрета хост падает сразу при старте с понятным сообщением (`OptionsValidationException: Jwt:Secret не задан...`), проверено вручную запуском `dotnet run` с `--environment=Production` (без dev-оверрайда).
- `BlizkaExceptionHandler` дополнен маппингом `UserDeletedException` → 410 (без `action`, т.к. это финальное состояние — аккаунт не восстановить через клиентское действие). Общий для миддлвари и хендлера код резолва locale вынесен в `RequestLocaleResolver` (было продублировано, теперь общее). Маппинг покрыт тестом `UserDeletedException_maps_to_410_without_action` в `BlizkaExceptionHandlerTests`.
- **Защита от гонки при первой авторизации.** Два почти одновременных `POST /api/auth/telegram` с одним `telegramId` могли одновременно не найти пользователя в `GetByTelegramIdAsync` и оба попытаться создать нового — второй `SaveChangesAsync` падал на уникальном индексе `IX_Users_TelegramId` необработанным `DbUpdateException` (→ 500). Теперь `UserRepository.SaveChangesAsync` ловит именно эту коллизию (по `PostgresException.SqlState == UniqueViolation` и имени констрейнта) и перебрасывает её как `ConcurrentUserCreationException` (`Blizka.App\Domain\Repositories`, без EF-зависимости в App-слое); `AuthenticateTelegramUserCommandHandler` ловит её и перезапрашивает уже созданного конкурентом пользователя вместо падения. Покрыто тестом `Handle_recovers_from_a_concurrent_user_creation_conflict` (с фейковым репозиторием, симулирующим конфликт).
- `AuthController.Telegram`: защитная ветка (когда `HttpContext.Items` неожиданно не содержит `TelegramInitData` — на практике недостижимо, т.к. middleware это гарантирует) раньше возвращала голый `Unauthorized()` без тела; приведена к общему контракту — `ApiErrorResponse` с кодом `TELEGRAM_INIT_DATA_INVALID` через `ErrorMessageCatalog`/`RequestLocaleResolver`, как и во всём остальном API.
- Покрытие тестами: `TelegramInitDataValidatorTests`, `JwtTokenServiceTests`, `AuthenticateTelegramUserCommandHandlerTests` (с фейковым `IUserRepository`/`IJwtTokenService`, без БД) — все в `Blizka.UnitTests`; `TelegramAuthMiddlewareTests` — в `Blizka.IntegrationTests` через сырой `TestServer` (по образцу `BlizkaExceptionHandlerTests`), не через `WebApplicationFactory<Program>`, т.к. последний потянул бы реальный `BlizkaDbContext`/Postgres. End-to-end путь (`dotnet run` → `POST /api/health` → `POST /api/auth/telegram` без заголовка → 401 с корректным телом) проверен вручную, но **создание/обновление пользователя через `AuthenticateTelegramUserCommandHandler` не проверено на живой БД** — как и в T-0.2, `docker compose up -d postgres` не запускался в момент реализации. Из-за этого маппинг `PostgresException` в `UserRepository.SaveChangesAsync` (конфликт по `TelegramId`) проверен только на уровне юнит-теста с фейковым репозиторием, а не с реальным Postgres.
- **Дополнение (после фидбека фронтенда, 2026-08-25): `status` в ответе `POST /api/auth/telegram` уходил в PascalCase (`"New"`), а не camelCase.** `AuthTelegramResponse.Status` был объявлен как `string`, а не `UserStatus` — значение приходило из `user.Status.ToString()` (`AuthenticateTelegramUserCommandHandler`), и глобальный `JsonStringEnumConverter(CamelCase)` (T-0.1) на него не срабатывает: конвертер применяется только к свойствам, типизированным самим enum'ом. Все остальные enum-поля во всём API уже были типизированы напрямую (например, `OnboardingCompleteResponse.UserStatus`) — это единственное исключение, найденное сквозным прогоном фронтенда. Исправлено: `AuthTelegramResponse.Status`/`AuthenticateTelegramUserResult.Status` перетипизированы в `UserStatus`.

**Зависимости:** T-0.1, T-0.2.

---

## Эпик 2 · Онбординг

### T-2.1 · Черновик онбординга `[MVP]`

**Экраны:** S-03, S-04, S-05.

**Результат:** Пошаговое сохранение данных регистрации с возможностью продолжить с того же места.

**Что сделать:**
- Таблица `OnboardingDraft` (userId, step, dataJson, updatedAt) или JSON-колонка в `User`.
- `PATCH /api/onboarding/draft` — принимает `{ step, data }`, перезаписывает данные шага.
- `GET /api/onboarding/draft` — возвращает текущий шаг и все сохранённые данные.
- Валидация по шагам:
  - Шаг 1 (S-03): `name` не пустое, `birthDate` → возраст ≥ 18, `gender` ∈ {male, female}.
  - Шаг 2 (S-04): `showGender` ∈ {female, male, all}, `ageRange` min < max, `datingGoals` непустой.
  - Шаг 3 (S-05): `cityId` существует в БД.
  - Шаг 4 (S-06): обрабатывается в T-3.1 (фото).
- Идемпотентность: повторный PATCH того же шага перезаписывает.

**Что сделано:**
- `PATCH`/`GET /api/onboarding/draft` реализованы вместе с T-2.3 (см. её "Что сделано" — обе задачи вводились одной волной, эта секция изначально не велась отдельно).
- **Дополнение (после фидбека фронтенда, 2026-08-25): `DELETE /api/onboarding/draft`.** В исходной задаче не было способа сбросить онбординг — ни удалить черновик, ни вернуть `User.Status`, из-за чего каждый тестовый прогон регистрации на стенде требовал нового Telegram-пользователя. `DeleteOnboardingDraftCommand`/`Handler` (`Blizka.App/UseCases/Onboarding`) удаляет черновик (если есть) и возвращает `Status` в `New`, если он был `Onboarding`/`Active` — сознательно не трогает начисленные зорки/фото/интересы/`UserFilter` (это debug-утилита, а не полное удаление аккаунта, для которого в будущем есть отдельный `DELETE /api/users/me/account`, T-16.1). `IOnboardingDraftRepository` получил `Remove(OnboardingDraft)`. По просьбе пользователя эндпоинт **открыт и в Production** (не ограничен `IHostEnvironment.IsDevelopment()`) — риск повторного начисления `RegistrationBonus` через цикл delete→redo-онбординг закрыт отдельно в T-2.3 (`RegistrationBonusAwardedAt`). Конкурентная запись того же `User` при сохранении переведена из сырого 500 в `OnboardingDraftResetConflictException` (409, `action: RETRY`) — по образцу остальных хендлеров, мутирующих `User`.
- **Дополнение (после фидбека фронтенда, 2026-08-25): `PatchOnboardingDraftRequest.Data` в OpenAPI-спеке был пустой схемой (`JsonElement` → `{}` — "любой JSON"), клиент не мог провалидировать тело до отправки.** Новый `OnboardingDraftDataSchemaTransformer` (`Blizka.Host/OpenApi` — единственный проект, ссылающийся на `Microsoft.AspNetCore.OpenApi`, поэтому трансформер живёт там, а не в `Blizka.Api`) подменяет схему поля `data` на `oneOf` трёх реальных форм шага (`OnboardingStep1Data`/`2Data`/`3Data`). Важно: подмена — это замена целого элемента `schema.Properties["data"]`, а не мутация того, что в нём лежало (`$ref` на общую именованную схему `JsonElement`, разделяемую с `OnboardingDraftResponse.Data`) — мутация задела бы и её. `OnboardingDraftResponse.Data` намеренно не тронут: это накопленные данные **нескольких** уже сохранённых шагов сразу (не одна форма), `oneOf` был бы неточным.
- **Дополнение (по тикету QA, 2026-08-25): `DELETE /api/onboarding/draft` теперь очищает и собственные свайпы пользователя.** Реальный сценарий на стенде: сброс черновика возвращал пользователя в состояние "сразу после онбординга", но сделанные им лайки/дизлайки оставались привязаны к аккаунту — повторно свайпнуть те же анкеты было нечем (уникальный индекс `Swipe(FromUserId, ToUserId)` блокировал повтор, а `undo` даёт лишь ограниченное число отмен подряд). `ISwipeRepository` получил `RemoveAllByUserAsync(fromUserId)` — удаляет только свайпы, сделанные этим пользователем (`FromUserId`), не трогает чужие свайпы на него и мэтчи. Вызывается из `DeleteOnboardingDraftCommandHandler` в общей транзакции с удалением черновика и сбросом статуса.

**Зависимости:** T-1.1, T-0.2.

---

### T-2.2 · Согласие пользователя `[MVP]`

**Экраны:** S-02.

**Результат:** Фиксация юридического согласия с временной меткой. ✅ Реализовано.

**Что сделать:**
- Таблица `UserConsent` (userId, type, version, timestamp, ipAddress, telegramId).
- `POST /api/users/me/consent` — запись согласия.
- Проверка при `POST /api/onboarding/complete`: без согласия → 422.
- Без чекбокса кнопка неактивна на фронте, но бэкенд тоже отклоняет — defense in depth.

**Что сделано:**
- `UserConsent` (`Blizka.App\Domain\Entities`) — append-only лог, а не одна перезаписываемая строка на пользователя: каждый `POST /api/users/me/consent` добавляет новую запись (`Guid Id` — суррогатный PK, в спеке не назван явно), так что при повторном согласии (например, с новой версией документа) история предыдущих согласий не теряется — это важно как юридическое доказательство. Поля — ровно те, что перечислены в задаче (`UserId`, `Type`, `Version`, `Timestamp`, `IpAddress`, `TelegramId`).
- `ConsentType` (`Blizka.App\Domain\Enums`) — **пока единственное значение `TermsAndPrivacyPolicy`**: интерфейс-спека для S-02 в репозитории отсутствует, а сама задача не перечисляет возможные типы согласия — по факту на этом экране один чекбокс на условия использования и политику конфиденциальности вместе. Enum (а не свободная строка) выбран по аналогии с остальными `Type`-полями в проекте (`SwipeType`, `SparkTransactionType` и т.п.).
- `TelegramId` — берётся не из БД, а из claim'а `telegramId` в JWT (уже кладётся туда `JwtTokenService` в T-1.1), через новый `ClaimsPrincipalExtensions.GetTelegramId()`. `IpAddress` — из `HttpContext.Connection.RemoteIpAddress`; в `Program.cs` нет `UseForwardedHeaders()`, так что за реверс-прокси (когда он появится) это нужно будет учесть отдельно — сейчас в проекте такого прокси-слоя ещё нет.
- `POST /api/users/me/consent` — новый `UsersController` (`Blizka.Api\Controllers`, `[Authorize]`, маршрут `api/users/me`) — первый контроллер под этим префиксом, задел под будущие `T-3.1`/`T-16.x`-эндпоинты вида `api/users/me/*`. Тело запроса `{ type, version }`; `userId`/`telegramId`/IP сервер берёт сам, клиент их не присылает.
- `RecordUserConsentCommand`/`Handler` (`Blizka.App\UseCases\Consent`) — по образцу `PatchOnboardingDraftCommandHandler`: `FluentValidation`-валидатор (`Type` — валидный enum, `Version` — не пусто) вызывается вручную внутри хендлера (в проекте нет общего MediatR `ValidationBehavior`), а не через pipeline.
- `IUserConsentRepository`/`UserConsentRepository` — помимо `AddAsync`/`SaveChangesAsync` уже содержит `HasConsentAsync(userId, type)` — сам эндпоинт T-2.2 им не пользуется, но это ровно та проверка, которая понадобится `POST /api/onboarding/complete` в T-2.3 ("без согласия → 422"); реализация — простой `AnyAsync` по индексу `(UserId, Type)`.
- **Проверка "без согласия → 422" при `POST /api/onboarding/complete` не реализована в этой задаче** — сам эндпоинт `/api/onboarding/complete` принадлежит T-2.3, которая ещё не сделана. `OnboardingIncompleteException` (422, код `ONBOARDING_INCOMPLETE`) уже существует в `Blizka.App\Domain\Exceptions` и замаплена в `BlizkaExceptionHandler` с более раннего момента — T-2.3 сможет использовать её и `HasConsentAsync` напрямую, без изменений в T-2.2.
- Миграция `AddUserConsent` (таблица `UserConsents`, FK на `Users` с `Cascade`, индекс `(UserId, Type)`) сгенерирована через `dotnet ef migrations add` и применена к локальному Postgres через `dotnet ef database update` — проверено вручную (`docker compose up -d postgres`), в отличие от T-1.1/T-0.2, где реальная БД не поднималась.
- Ручная проверка (включая живой прогон `dotnet run --project src/Blizka.Host` против реального Postgres с настоящей строкой `Users`, а не только через fake-репозитории в тестах — токен подписан вручную тем же dev-секретом из `appsettings.Development.yaml`, т.к. в репозитории нет готового способа сгенерировать валидный Telegram `initData` для `POST /api/auth/telegram`) на код-ревью после реализации вскрыла две реальные проблемы, обе исправлены и закрыты регрессионными тестами:
  - **`Version` длиннее 32 символов (лимит колонки `UserConsent.Version`) падал в 500**, а не в чистую 400-ошибку — `NotEmpty()` в `RecordUserConsentCommandValidator` не проверял длину, поэтому Postgres сам бросал `value too long for type character varying(32)` уже на `SaveChangesAsync`, а `BlizkaExceptionHandler` не маппит `DbUpdateException` ни на что, кроме общего 500. Исправлено добавлением `.MaximumLength(32)` к правилу для `Version`.
  - **Невалидное значение `type` в теле запроса возвращало ASP.NET-овский `ValidationProblemDetails` вместо `ApiErrorResponse`** — `RecordConsentRequest.Type` биндится напрямую как `ConsentType`, и невалидная строка валится ещё на этапе JSON-биндинга, до FluentValidation (`Type.IsInEnum()` в валидаторе в результате мёртвый код — до него дело не доходит). Это первый эндпоинт в проекте с enum-полем, забинженным прямо в DTO верхнего уровня (в отличие от онбординга, где шаговые данные приходят как `JsonElement` и парсятся вручную) — раньше этот системный пробел был не виден. Исправлено не точечно для этого эндпоинта, а на уровне всего API-слоя: `ApiServiceCollectionExtensions.AddApiLayer` теперь настраивает `ConfigureApiBehaviorOptions(...).InvalidModelStateResponseFactory`, оборачивая любой сбой автоматической валидации модели `[ApiController]` в тот же `ApiErrorResponse`/`VALIDATION_ERROR`, что и `BlizkaExceptionHandler` — так что все будущие эндпоинты с строго типизированными enum/DTO-полями в теле запроса получают единый контракт ошибок бесплатно.
- Тесты: `RecordUserConsentCommandHandlerTests` (`Blizka.UnitTests`, фейковый `IUserConsentRepository`) и `UsersControllerTests` (`Blizka.IntegrationTests`, минимальный тестовый хост с реальным JWT bearer/`[Authorize]`, по образцу `OnboardingControllerTests`) — включая регрессионные кейсы на обе найденные при код-ревью проблемы (`Version` длиннее лимита колонки, нераспознанный `type`).
- **Дополнение (после фидбека фронтенда, 2026-08-23): `GET /api/users/me/consent`** — в исходной задаче не было ни этого эндпоинта, ни способа вообще узнать статус согласия иначе, чем по `OnboardingDraft.Step` (хрупкая связь для клиента). `GetUserConsentStatusQuery`/`Handler` (`Blizka.App\UseCases\Consent`) возвращает статус по каждому значению `ConsentType` (сейчас — одному), беря самую свежую запись по `Timestamp` из append-only лога через новый `IUserConsentRepository.GetByUserIdAsync`; типа без единой записи — `Given: false`, а не 404 (тот же принцип "пустое состояние", что и `GET /api/onboarding/draft`/`GET /api/feed/filters`).

**Зависимости:** T-1.1.

---

### T-2.3 · Завершение онбординга и начисление зорок `[MVP]`

**Экраны:** S-07.

**Результат:** Переход `onboarding → active`, начисление стартовых зорок. ✅ Реализовано.

**Что сделать:**
- `POST /api/onboarding/complete`:
  - Проверить: все 4 шага заполнены, согласие дано, минимум 1 фото загружено.
  - Статус `User.Status` → `Active`.
  - Начислить ✦50 (создать `SparkTransaction` с типом `RegistrationBonus`).
  - Рассчитать `ProfileCompleteness` (35% после базового онбординга).
  - Вернуть: `sparksAwarded`, `profileCompleteness`, `nextReward`.
- Логика расчёта `ProfileCompleteness`:
  - Имя, возраст, пол, город, фото (1+), цель, фильтры = 35%.
  - +15% за 3+ фото, +10% за 5+ интересов, +10% за промпты, +10% за предпочтения на свидания, +10% за верификацию, +5% за голосовое, +5% за Instagram.
- При достижении порогов (60%, 80%, 100%) — начисление бонусных зорок (отдельная проверка).

**Что сделано:**
- `CompleteOnboardingCommandHandler` (`Blizka.App\UseCases\Onboarding`), вызывается из нового `OnboardingController.Complete` (`POST /api/onboarding/complete`). Читает `OnboardingDraft.DataJson` **целиком** (не по шагам, как `PatchOnboardingDraftCommandHandler`) — десериализует в `CombinedOnboardingData` (шаги 1-3 слиты в один плоский объект) и проверяет обязательные поля каждого шага явно, а не по `OnboardingDraft.Step >= 3`: `PATCH /api/onboarding/draft` не требует последовательного заполнения шагов на сервере, так что `Step` мог быть выставлен в 3, а данные шага 1 или 2 — отсутствовать.
- Порядок проверок (каждая — отдельный `OnboardingIncompleteException(missingStep)`, 422/`ONBOARDING_INCOMPLETE`, уже промаппленный в T-2.2): шаг 1 → шаг 2 → шаг 3 → фото (`missingStep`: `step1`/`step2`/`step3`/`step4`) → согласие (`missingStep: consent`, через уже готовый `IUserConsentRepository.HasConsentAsync` из T-2.2).
- **Перенос данных черновика в `User` — этим эндпоинтом делается впервые.** До T-2.3 `PatchOnboardingDraftCommandHandler` писал данные только в `OnboardingDraft.DataJson`, ни разу не трогая сам `User`. `Complete` копирует `Name`/`BirthDate`/`Gender`/`CityId` напрямую. **`DatingGoal` — судьбоносное упрощение:** шаг 2 позволяет выбрать несколько целей (`DatingGoals: DatingGoal[]`), а у `User` только одно поле `DatingGoal` — берётся первая выбранная как основная.
- **`ShowGenderPreference`/`AgeRange` (тоже часть шага 2) сознательно никуда не переносятся** — у `User` нет для них поля, а `UserFilter` из T-5.4 ещё не создан (см. заметку T-0.2). Они остаются только в `OnboardingDraft.DataJson`; когда появится T-5.4, миграцию/перенос этих данных нужно будет продумать отдельно. Соответственно и пункт "фильтры" в формуле 35%-базы **не проверяется отдельно** — раз шаг 2 прошёл валидацию при заполнении черновика, этот пункт считается выполненным автоматически вместе с остальной частью базовых 35%.
- **T-8.1 (кошелёк зорок) не реализован как отдельная задача** — `ISparksService.Award/Spend/GetBalance/GetHistory`, `GET /api/sparks/wallet` и атомарный `UPDATE ... WHERE sparks_balance >= @amount` из его спеки отсутствуют. Для T-2.3 добавлен только минимально необходимый `ISparkTransactionRepository` (`AddAsync`/`SaveChangesAsync`, `Blizka.Data\Repositories\SparkTransactionRepository`) — начисление делается прямо в хендлере (`user.SparksBalance += amount` + новая запись `SparkTransaction`, без отдельного гонко-безопасного `UPDATE`, т.к. в T-2.3 нет конкурентного списания). Полноценный `ISparksService` — предмет отдельной реализации T-8.1.
- **Суммы бонусов за пороги ProfileCompleteness (60/80/100%) явно не заданы decomposition.md** — взяты по аналогии со строкой T-8.1 "Таблица начислений (из spec раздел 15.2): registration 50, profile 2+2+2, ..." → 3 порога по ✦2 каждый (`SparkTransactionType.ProfileCompletion`).
- **Защита от повторного начисления порогового бонуса** — через уже существовавшие в `User` поля `CompletenessBonus60/80/100AwardedAt` (заведены заранее в T-0.2, до этой задачи нигде не читались/не писались). Начисление конкретного порога происходит только если поле ещё `null`.
- **Идемпотентность самого эндпоинта — сначала последовательная (по `Status`), потом и по гонке.** Первая версия проверяла только `User.Status != New` → `OnboardingAlreadyCompletedException` (409, `ONBOARDING_ALREADY_COMPLETED`) — это ловит повторный вызов, но не два **параллельных** `POST /complete` для одного пользователя: оба запроса читают `Status == New` до того, как любой из них закоммитится, оба проходят проверки и оба начисляют ✦50, задваивая `SparkTransaction` в леджере (сам `SparksBalance` при этом мог случайно остаться "правильным" из-за одинаковой базы расчёта — леджер и баланс расходятся незаметно). Найдено на код-ревью, закрыто оптимистичной блокировкой: `UserConfiguration` добавляет теневое свойство `builder.Property<uint>("xmin").IsRowVersion()`, смэппленное на системную колонку Postgres `xmin` (обновляется самим Postgres при каждом UPDATE строки, новой колонки не заводит). `UserRepository.SaveChangesAsync` ловит `DbUpdateConcurrencyException` и перебрасывает как `ConcurrentUserUpdateException` (по образцу `ConcurrentUserCreationException`/`ConcurrentOnboardingDraftCreationException`); `CompleteOnboardingCommandHandler` ловит её вокруг финального `SaveChangesAsync` и превращает во всё тот же `OnboardingAlreadyCompletedException` — проигравший гонку запрос получает тот же 409, что и обычный повторный вызов, без задвоенного начисления. Миграция `AddUserXminConcurrencyToken` — Up/Down намеренно пустые: сгенерированный по умолчанию `dotnet ef migrations add` код пытался `ALTER TABLE "Users" ADD COLUMN "xmin"`, а Postgres отклоняет колонку с именем системной (`column name "xmin" conflicts with a system column name`); руками оставлены только метаданные модели (Designer.cs/snapshot). Проверено на реальном Postgres (`docker compose up -d postgres` + `dotnet ef database update` + одноразовый тест с двумя параллельными `DbContext`, подтвердивший `ConcurrentUserUpdateException` на второй `SaveChangesAsync`) — единственный кусок T-2.3, реально прогнанный против живой БД, а не только через фейковые репозитории.
- `IUserRepository` дополнен `GetByIdWithProfileDataAsync` (имя специально не `GetByIdAsync` — грузит `User` вместе с `Photos`/`UserInterests` через `Include`, что нужно именно этому сценарию; общий "голый" `GetByIdAsync` можно будет завести отдельно, когда появится вызывающий, которому лишние `Include` не нужны). `IUserDatePreferenceRepository.CountByUserIdAsync` — новый, минимальный, только для формулы completeness (полноценного CRUD для `UserDatePreference` пока нигде нет, т.к. нет эндпоинта для выбора предпочтений на свидания).
- Разбор `OnboardingDraft.DataJson` обёрнут в `try/catch (JsonException)` (по образцу `PatchOnboardingDraftCommandHandler.Deserialize`) — повреждённые/неожиданной формы данные черновика превращаются в обычный 422 `ONBOARDING_INCOMPLETE` (`missingStep: step1`), а не в 500.
- Тесты: `CompleteOnboardingCommandHandlerTests` (`Blizka.UnitTests`, 10 сценариев — минимальный/полный профиль, отсутствие черновика/фото/согласия, повторное завершение (последовательное и по гонке через `ConcurrentUserUpdateException`), корректная идемпотентность порогового бонуса при уже достигнутом пороге) и 4 сценария в `OnboardingControllerTests` (`Blizka.IntegrationTests`, тот же минимальный тестовый хост, что и у `PATCH`/`GET draft`) — 401 без токена, 200 со счастливым путём, 422 без согласия, 409 при повторном вызове. На реальной БД помимо xmin-проверки (см. выше) не проверялось — по той же причине, что и в T-1.1/T-0.2.
- **Дополнение (после фидбека фронтенда, 2026-08-25): `nextReward.hint` приходил на языке регистрации, а не на языке текущего запроса.** `ProfileCompletenessCalculator.NextReward` резолвил локаль из персистентного `User.Locale` (зафиксирован один раз при `POST /api/auth/telegram` из `initData.LanguageCode`, T-1.1) — расходится с остальными локализованными полями того же ответа, которые резолвятся через `RequestLocaleResolver` (JWT-claim, затем `Accept-Language`, T-0.3). `CompleteOnboardingCommand` получил параметр `Locale`; `OnboardingController.Complete` резолвит его тем же `RequestLocaleResolver`, что и все остальные сообщения об ошибках, и передаёт в команду вместо того, чтобы хендлер читал `user.Locale` напрямую.
- **Дополнение (после фидбека фронтенда, 2026-08-25): `RegistrationBonusAwardedAt` — защита от повторного начисления регистрационного бонуса.** Появилась вместе с `DELETE /api/onboarding/draft` (T-2.1): без неё цикл "сбросить онбординг → пройти PATCH заново → `POST /complete`" начислял бы `RegistrationBonusAmount` (✦50) на каждый круг — в отличие от порогов `ProfileCompleteness`, у `RegistrationBonus` изначально не было idempotency-guard'а (не было и повода: до `DeleteDraft` `Status` не мог вернуться в `Onboarding` после `Active`). Новое поле `User.RegistrationBonusAwardedAt` (по образцу `CompletenessBonus60/80/100AwardedAt`) + миграция `T_RegistrationBonusAwardedAt` — начисление происходит только если поле ещё `null`.

**Зависимости:** T-2.1, T-2.2, T-3.1, T-8.1.

---

## Эпик 3 · Фотографии

### T-3.1 · Загрузка и управление фото `[MVP]`

**Экраны:** S-06.

**Результат:** Upload, хранение, удаление, переупорядочивание фото. ✅ Реализовано.

**Что сделать:**
- `POST /api/users/me/photos` — multipart upload, сохранение в S3-совместимое хранилище.
- `DELETE /api/users/me/photos/{photoId}`.
- `PATCH /api/users/me/photos/reorder` — `{ order: [id1, id2, ...], mainPhotoId }`.
- При загрузке: удалить EXIF из файла на сервере (библиотека `MetadataExtractor` или `SixLabors.ImageSharp`).
- Ресайз: генерация thumbnail (150px) и medium (600px).
- Ограничения: max 6 фото, max 10MB на файл, форматы jpg/png/webp.
- `POST /api/users/me/photos/import-telegram` — скачать аватар по `user.photo_url` из Telegram.

**Что сделано:**
- **Хранилище — MinIO локально, AWSSDK.S3 как клиент** (по просьбе пользователя). `Storage`-секция в `appsettings.yaml` уже существовала (заведена заранее, до этой задачи нигде не читалась) и один-в-один легла на нужды S3-совместимого клиента (`Endpoint`/`Bucket`/`AccessKey`/`SecretKey`/`PublicBaseUrl`). В `docker-compose.yml` добавлены `minio` (образ `minio/minio:RELEASE.2025-09-07T16-13-09Z-cpuv1`, healthcheck `mc ready local`) и одноразовый `minio-init` (по образцу `migrator`) — создаёт бакет `blizka-photos` и включает анонимное скачивание (`mc anonymous set download`), так что публичные URL фото отдаются из MinIO напрямую, без проксирования через API. `IAmazonS3` регистрируется в `AddDataLayer` с `ForcePathStyle = true` (обязательно для MinIO) и `BasicAWSCredentials` из конфига.
- **Библиотека обработки изображений — SixLabors.ImageSharp, выбор подтверждён пользователем явно.** CLAUDE.md фиксирует прецедент отказа от FluentAssertions из-за платной коммерческой лицензии выше порога выручки (Six Labors Split License устроена аналогично — бесплатно только до $1M выручки/для не-OSS). Перед добавлением пакета пользователю был задан прямой вопрос с альтернативой (Magick.NET, Apache 2.0, без порога выручки) — пользователь осознанно выбрал ImageSharp, т.к. он явно назван в самой задаче decomposition.md. Лицензионный риск не устранён, а сознательно принят — при пересмотре в будущем стоит перечитать этот пункт.
- `PhotoImageProcessor` (`Blizka.App\Photos`, статический, без ASP.NET Core/EF Core — та же логика, что и для `NetTopologySuite` в CLAUDE.md) — декодирует файл, применяет `AutoOrient()` **до** удаления EXIF (иначе фото, снятые с поворотом, лежали бы на боку — ориентация зашита именно в EXIF), затем снимает `ExifProfile`/`IccProfile`/`XmpProfile`. Оригинал переоценивается в исходном формате (jpg/png/webp) с удалённой метадатой, без изменения разрешения; thumbnail (150px) и medium (600px) — **всегда JPEG независимо от исходного формата** (решение не из спеки — ради предсказуемого размера и единообразной отдачи клиенту), `ResizeMode.Max` по большей стороне. Формат, не входящий в jpg/png/webp (распознанный, но не поддерживаемый, например BMP), и повреждённые/нераспознаваемые файлы дают одинаковый `FluentValidation.ValidationException` → 400, а не 500 — по образцу `PatchOnboardingDraftCommandHandler.Deserialize`, который так же превращает "плохие" входные данные в `ValidationException` вручную вместо отдельного типа исключения.
- **Схема ключей объектов** — `photos/{userId:N}/{photoId:N}/{original|thumbnail|medium}.{ext}` (`PhotoStorageKeys`, `Blizka.App\Photos`), общая для загрузки и удаления. Расширение оригинала при удалении не хранится отдельным полем в `Photo` (сущность не менялась с T-0.2 — только `Url`/`ThumbnailUrl`/`MediumUrl`/`SortOrder`/`IsMain`), а парсится из `Photo.Url` (`PhotoStorageKeys.ExtensionFromUrl`) — сознательный выбор не добавлять новую колонку ради детерминированного и так значения.
- `IPhotoRepository` — намеренно **без** отдельного `GetByIdAsync(photoId)`: единственный способ найти фото — `GetByUserIdAsync(userId)`, отфильтрованный на сервере по `userId` из JWT. Так чужое фото не отличимо от несуществующего (IDOR-защита) на уровне контракта репозитория, а не только проверкой в хендлере.
- **Лимит в 6 фото** — `PhotoLimitExceededException` (422, код `PHOTO_LIMIT_EXCEEDED`, action `DELETE_A_PHOTO`). **Автоматическое назначение главного фото** — первое загруженное становится главным (`existingCount == 0`); при удалении текущего главного, если у пользователя остались другие фото, главным становится следующее по `SortOrder` (`DeletePhotoCommandHandler`) — в задаче явно не описано, но иначе профиль на время остался бы без обложки.
- **`POST /api/users/me/photos/import-telegram` — SSRF-защита через allowlist хоста.** `photoUrl` присылает клиент запросом (см. ниже, почему), а сервер скачивает файл по нему — без ограничения хоста это прямой SSRF (можно было бы заставить сервер обратиться к любому адресу, включая внутреннюю инфраструктуру). `ImportTelegramPhotoCommandValidator` требует `https` и хост ровно `t.me`. Проверено вручную: `http://169.254.169.254/latest/meta-data/` отклоняется валидатором до сетевого вызова.
- **`photoUrl` не хранится на сервере — потому что T-1.1 сознательно не сохранил `photo_url` из Telegram initData** (см. заметку в этом же файле к T-1.1: "скачивание файла... принадлежат `POST /api/users/me/photos/import-telegram` из T-3.1"). На момент импорта актуальное значение есть только у клиента (`Telegram.WebApp.initDataUnsafe.user.photo_url`), поэтому `ImportTelegramPhotoRequest.PhotoUrl` — обязательное поле тела запроса, а не что-то читаемое из БД/JWT.
- `ImportTelegramPhotoCommandHandler` скачивает файл через `ITelegramAvatarDownloader` (реализация `Blizka.Data\Http\TelegramAvatarDownloader`, `HttpClient` с таймаутом 10с и ручным ограничением объёма чтения в 10MB — Content-Length от Telegram CDN не гарантирован) и **переиспользует `UploadPhotoCommand` через `IMediator.Send`** вместо дублирования лимита/обработки/аплоада — единственное место в проекте, где хендлер вызывает `mediator.Send` из другого хендлера (осознанный выбор ради DRY, а не паттерн для копирования по умолчанию).
- **Реальный баг, пойманный только ручным тестом на живом MinIO (не юнит-тестами с фейками):** `PutObjectRequest.DisablePayloadSigning = true` требует HTTPS — с локальным MinIO по `http://localhost:9000` любая загрузка падала в 500 (`AmazonClientException: When DisablePayloadSigning is true, the request must be sent over HTTPS`). Убрано; S3 SDK и так сам подписывает payload корректно без этого флага. Полный happy path (`docker compose up -d minio minio-init postgres` + `dotnet run` + curl multipart upload реального JPEG с EXIF-ориентацией) прогнан вручную: EXIF/ICC действительно снимаются (проверено побайтовым поиском строки, зашитой в тестовый EXIF, и маркера `Exif` в скачанном файле — оба отсутствуют), thumbnail/medium — корректных максимальных размеров, все три URL публично отдаются из MinIO, 6-й лимит, 404 на чужое/несуществующее фото, переназначение главного фото при удалении и SSRF-отказ подтверждены отдельными запросами.
- Тесты: `PhotoImageProcessorTests`, `UploadPhotoCommandHandlerTests`, `DeletePhotoCommandHandlerTests`, `ReorderPhotosCommandHandlerTests`, `ImportTelegramPhotoCommandHandlerTests` (`Blizka.UnitTests`, фейковые репозиторий/хранилище/загрузчик; `ImportTelegramPhotoCommandHandlerTests` использует рукописный `SingleHandlerMediator`, форвардящий только `Send<TResponse>(IRequest<TResponse>)` — единственный член `IMediator`, которым пользуется хендлер импорта, т.к. в проекте нет библиотеки моков) и `PhotosControllerTests` (`Blizka.IntegrationTests`, тот же минимальный тестовый хост, что и `UsersControllerTests`, включая настоящий multipart-биндинг `IFormFile`).
- **Пост-ревью правки (`/code-review`), все проверены на живом Postgres/MinIO, не только юнит-тестами:**
  - **Гонка на лимите/главном фото при параллельной загрузке.** `PhotoConfiguration` теперь объявляет `(UserId, SortOrder)` и `(UserId, IsMain=true)` как **unique**-индексы (миграция `MakePhotoIndexesUnique`; раньше первый был обычным, второго не было вовсе) — до этого два одновременных `POST /photos` от одного пользователя (двойной тап на медленной сети) могли оба прочитать `existingCount == 0` и оба записаться как главное фото с `SortOrder = 0`. `PhotoRepository.SaveChangesAsync` ловит нарушение этих индексов (по имени констрейнта, как `UserRepository`) и перебрасывает как новый `ConcurrentPhotoUploadException` (`Blizka.App\Domain\Repositories`, по образцу `ConcurrentUserCreationException`); `UploadPhotoCommandHandler.SaveWithRetryAsync` ловит её, пересчитывает `SortOrder`/`IsMain` по свежему `CountByUserIdAsync` и повторяет `SaveChangesAsync` **на том же `DbContext`** (до `MaxConcurrencyAttempts = 3`) — эмпирически проверено, что повторный `SaveChangesAsync` после неудачного на одном контексте у Npgsql/EF Core действительно отрабатывает, это не было прецедентом в проекте (T-2.3/T-1.1 переигрывают через повторное чтение, а не повторный `SaveChangesAsync`). Если бюджет попыток исчерпан — что при реалистичном двойном тапе практически недостижимо, но легко ловится намеренным стресс-тестом на 5+ параллельных запросов — `ConcurrentPhotoUploadException` переводится в обычный `PhotoUploadConflictException` (409, код `PHOTO_UPLOAD_CONFLICT`, action `RETRY_UPLOAD`), а не утекает наружу необработанной (сначала утекала — поймано именно ручным стресс-тестом через `curl` в 4-6 параллельных запросов, не юнит-тестами с фейками, которые гонку не воспроизводят). `BlizkaDomainException` для этого получил необязательный `innerException`, чтобы `PhotoUploadConflictException` не терял исходный `PostgresException` в логах.
  - **SSRF-защита `import-telegram` не покрывала редиректы.** `ImportTelegramPhotoCommandValidator` проверяет только исходный `photoUrl`, а `HttpClient` по умолчанию сам следует за 3xx-редиректами — ответ `t.me` с `Location` на произвольный хост обошёл бы allowlist незаметно для валидатора. `TelegramAvatarDownloader` теперь регистрируется с `SocketsHttpHandler { AllowAutoRedirect = false }` в `AddDataLayer`.
- **Дополнение (после фидбека фронтенда, 2026-08-23): `GET /api/users/me/photos`** — в исходной задаче списка не было, только upload/delete/reorder/import — после перезагрузки страницы клиент не мог увидеть уже загруженные фото. `GetPhotosQuery`/`Handler` (`Blizka.App\UseCases\Photos`) переиспользует существующий `IPhotoRepository.GetByUserIdAsync` (уже отсортирован по `SortOrder`) и маппинг `UploadPhotoCommandHandler.ToResult` — новой логики в App-слое почти нет.
  - Убрано лишнее двойное буферирование в `ImportTelegramPhotoCommandHandler` (копирование уже полностью буферизованного `download.Content` в новый `MemoryStream`) — интерфейс `ITelegramAvatarDownloader`/`TelegramAvatarDownload` теперь явно документирует, что `Content` уже seekable с доступной `Length`.
  - `StorageOptions` (Endpoint/Bucket/PublicBaseUrl) получил `ValidateOnStart()` — по аналогии с `Jwt:Secret` в `ApiServiceCollectionExtensions`, чтобы неполный конфиг падал при старте хоста, а не 500-кой на первой реальной загрузке фото.
- **Дополнение (после фидбека фронтенда, 2026-08-25): фото не открывались по ссылкам из ответа API — 404 у всех.** Причина — не код, а конфигурация: `Storage:Endpoint` в Railway был задан с путём бакета (`.../blizka-photos`) вместо голого origin MinIO. `AmazonS3Client` с `ForcePathStyle = true` (обязателен для MinIO, T-3.1) сам подставляет `/{Bucket}/{Key}` к `ServiceURL` при каждом запросе — с уже включённым в `Endpoint` именем бакета объект физически ложился по задвоенному пути (`bucket=blizka-photos`, `key=blizka-photos/photos/...`), а публичная ссылка строится из отдельного `PublicBaseUrl` и была "правильной" — расхождение никак не проявлялось до первого реального обращения по URL. Сам Railway env var и уже испорченные объекты в бакете — вне зоны действия кода, чинятся отдельно на инфраструктуре. Код получил fail-fast guard: `DataServiceCollectionExtensions` добавляет `.Validate(...)` к `StorageOptions`, запрещающий `Endpoint`, оканчивающийся на `/{Bucket}` — при такой конфигурации хост не поднимется, а не будет молча портить объекты.
- **Дополнение (после фидбека фронтенда, 2026-08-25): `POST /api/users/me/photos/import-telegram` возвращал 500 вместо понятного клиенту кода, если аватар недоступен.** `TelegramAvatarDownloader.DownloadAsync` пробрасывал сырые `HttpRequestException`/оверсайз-`InvalidOperationException`, `BlizkaExceptionHandler` не мапит framework-исключения ни на что, кроме общего `INTERNAL_ERROR`/500. Новый `TelegramAvatarDownloadFailedException` (`PHOTO_DOWNLOAD_FAILED`, 422) оборачивает любую ошибку скачивания (недоступный/протухший URL, обрыв соединения, таймаут `HttpClient` — отличается от отмены самим вызывающим по состоянию `CancellationToken`, превышение лимита размера) — клиент теперь может отличить "картинки нет" от аварии сервера.

**Зависимости:** T-0.2, T-1.1.

---

### T-3.2 · Автопроверка фото `[POST-MVP]`

**Экраны:** S-06 (notes).

**Результат:** Pipeline проверки загруженных фото.

**Что сделать:**
- Очередь проверки: при загрузке фото → статус `Pending`, задача в очередь.
- Background job `PhotoModerationQueue` (каждые 5 мин).
- Проверки:
  - NSFW-детектор (ML.NET или внешний API) → `nsfwScore > 0.5` → `Rejected`.
  - Face detection → `faceDetected: false` → предупреждение (не может быть главным).
  - Перцептивный хэш (pHash) → сравнение с базой стоковых фото → `Rejected`.
- Сообщение об ошибке объясняет, что делать: «Не видно лица — загрузите другое фото».

**Зависимости:** T-3.1.

---

## Эпик 4 · Города и геолокация

### T-4.1 · Поиск городов `[MVP]`

**Экраны:** S-05.

**Результат:** Полнотекстовый поиск по населённым пунктам. ✅ Реализовано.

**Что сделать:**
- Seed таблицы `City` — все населённые пункты Беларуси + крупные города Польши, Литвы, Латвии, России, Украины (диаспора).
- `GET /api/cities/search?q=Мінск&locale=ru` — trigram search (`pg_trgm`), limit 10.
- `POST /api/geo/detect` — reverse geocoding по координатам (Nominatim OSM или аналог).
- Ответ включает `isOpen` для каждого города (MVP: все города открыты, механика waitlist — post-MVP).

**Что сделано:**
- **Сидинг диаспоры — не "все населённые пункты", а 13 крупных городов** (`CitySeed`, `Blizka.Data\Seed`): Варшава/Краков/Вроцлав/Гданьск/Белосток (PL), Вильнюс/Каунас (LT), Рига (LV), Москва/Санкт-Петербург/Смоленск (RU), Киев/Львов (UA) — задача говорит "крупные города", полного перечня решено было не заводить (несоразмерный объём для MVP). Каталог Беларуси из T-0.2/T-4.1-заготовки (28 городов) не тронут, только добавлены новые строки — миграция `AddDiasporaCities` (`InsertData`, GUID продолжает существующую детерминированную последовательность `00000000-0000-0000-0a02-...`, применена и проверена на реальном Postgres). Итого в каталоге 41 город.
- `GET /api/cities/search` (`CitiesController`, `[Authorize]`) — `SearchCitiesQuery`/`Handler` (`Blizka.App\UseCases\Cities`) вызывает `ICityRepository.SearchAsync`, которая транслируется Npgsql-провайдером в `EF.Functions.TrigramsSimilarity` по одной из трёх колонок (`NameRu`/`NameBe`/`NameEn` — выбор колонки зависит от `locale`, ветки `switch` держат имя колонки статическим ради трансляции в SQL). **Порог подобия — 0.15, не дефолтный GUC `pg_trgm.similarity_threshold` (0.3)**: с дефолтным порогом короткие запросы (2-3 буквы), типичные при наборе текста, не находили ничего — проверено вручную на реальном Postgres (`docker compose up -d postgres`, `dotnet ef database update`, живой `dotnet run` + curl с реальным JWT, подписанным dev-секретом). `locale` — новый `CityLocale` (`Blizka.App\Domain\Enums`, отдельный от `ApiLocale` из T-0.3: тот про локаль сообщений об ошибках API-слоя, этот — про выбор колонки имени в App/Data-слоях, общий enum между ними противоречил бы направлению зависимостей) и `CityLocaleParser` (`Blizka.Api\Cities`, копия формата `ApiLocaleParser` с дефолтом `ru`, но отдельный класс по той же причине).
- `POST /api/geo/detect` (`GeoController`, `[Authorize]`) — **сознательно не полагается на текстовое совпадение с ответом Nominatim.** Подбор ближайшего города каталога идёт через `ICityRepository.FindNearestAsync` — чистый PostGIS-запрос (`geography.Distance`, транслируется в `ST_Distance`, метры — ради чего колонка `Coordinates` в T-0.2/T-4.1-заготовке и так уже была `geography`, а не `geometry`) с отсечкой 50км, без обращения к Nominatim вовсе. Nominatim (`INominatimGeocoder`/`NominatimGeocoder`, `Blizka.Data\Geo`, `HttpClient` по образцу `ITelegramAvatarDownloader` из T-3.1) используется только для человекочитаемого `detectedAddress` в ответе — сопоставлять его `display_name` с написанием городов в сидинге ненадёжно (разная транслитерация: OSM для белорусских городов может использовать не тот вариант, что выбран в `CitySeed`). Вызов Nominatim обёрнут в `try/catch` в `DetectCityQueryHandler` — сбой/недоступность внешнего сервиса не должны валить весь эндпоинт, раз подбор города от него не зависит; проверено вручную реальным вызовом публичного Nominatim (`https://nominatim.openstreetmap.org`) для координат Минска — вернул `"Мінск, Беларусь"`.
- **`Geo`-секция appsettings** (`GeoOptions`, `Blizka.Data\Geo`) — `NominatimBaseUrl` (дефолт — публичный `nominatim.openstreetmap.org`) и обязательный `NominatimUserAgent` (`ValidateOnStart()`, по аналогии со `StorageOptions`): usage policy публичного Nominatim требует опознаваемый User-Agent, иначе сервис блокирует запросы по IP. Значение в `appsettings.yaml` — плейсхолдер (`Blizka/1.0 (Telegram dating mini-app)`), помечено комментарием на замену перед продакшн-деплоем реальным контактом.
- **`[Authorize]` на обоих контроллерах** — в задаче явно не указано, но выбрано для единообразия со всеми существующими контроллерами (`UsersController`, `OnboardingController`), у которых нет анонимных эндпоинтов, кроме `POST /api/auth/telegram`.
- Ручная проверка на реальном Postgres и реальном Nominatim (см. выше) прогнана против всех веток: поиск на кириллице и латинице (в т.ч. по диаспоре, например `Vil` → `Vilnius`/`Vileyka`/`Vitsyebsk`), пустой `q` → 400 `VALIDATION_ERROR`, `/detect` рядом с каталожным городом и вдали от всех (`lat=0, lon=0` → `city: null`), `/detect` с `lat` вне диапазона → 400.
- Тесты: `SearchCitiesQueryHandlerTests`/`DetectCityQueryHandlerTests` (`Blizka.UnitTests`, фейковые `ICityRepository`/`INominatimGeocoder`, включая кейс "геокодер бросает исключение — не прерывает обработку") и `CitiesControllerTests`/`GeoControllerTests` (`Blizka.IntegrationTests`, тот же минимальный тестовый хост, что и `PhotosControllerTests`).
- **Пост-ревью правки (`/code-review`), все проверены на живом Postgres/Nominatim, не только юнит-тестами:**
  - **Нет throttling исходящих запросов к Nominatim — реальный операционный риск при росте трафика.** Публичный Nominatim держит лимит 1 запрос/сек **с одного IP**, а источник у всех запросов бэкенда один — всплеск одновременных `/api/geo/detect` (несколько регистраций подряд, вполне реалистично уже на паре сотен DAU) мог забанить по IP весь бэкенд разом, а не одного пользователя. Добавлен новый пакет `System.Threading.RateLimiting` (версия `10.0.11`, взята через `dotnet add package` по процедуре CPM из этого файла, т.к. отдельным NuGet-пакетом он не был в кэше — в отличие от `System.Threading.RateLimiting`, зашитого в `Microsoft.AspNetCore.App`, куда `Blizka.Data` шаринг-фреймворк не тянет) — общий на всё приложение `FixedWindowRateLimiter` (1 permit/сек, очередь на 3) регистрируется синглтоном в `AddDataLayer` и внедряется в `NominatimGeocoder`. Что не влезло в очередь — не ждёт и не роняет запрос, а просто не обогащается адресом (`detectedAddress: null`), т.к. это и так не критичный путь. Проверено вручную 6 параллельными `curl`-запросами: 3 получили адрес (растянуто по ~1 сек друг от друга), 3 сразу получили `null` без ожидания — во всех 6 `city` и HTTP 200 не пострадали.
  - **`FindNearestAsync` и `geocoder.ReverseGeocodeAsync` в `DetectCityQueryHandler` теперь выполняются параллельно** (`Task.WhenAll`), а не один за другим — они независимы (БД и внешний HTTP).
  - **`NominatimGeocoder` теперь передаёт `accept-language`, выведенный из `CityLocale`** запроса — раньше `detectedAddress` приходил в языке, который выбирал сам Nominatim, а не в запрошенной локали. `INominatimGeocoder.ReverseGeocodeAsync` получил параметр `CityLocale locale`. Проверено вручную: один и тот же `(lat, lon)` с `locale=en` вернул `"Minsk, Belarus"`, с `locale=ru` — `"Минск, Беларусь"`.
  - **Исправлена реальная логическая ошибка в обработке отказа геокодера**, найденная при пересмотре фильтра исключений: изначальный `catch (Exception ex) when (ex is not OperationCanceledException)` **пропускал бы наружу необработанным как раз таймаут `HttpClient` (5с)** — он тоже бросает `TaskCanceledException`, наследника `OperationCanceledException`, а значит не попадал бы под catch и уронил бы весь `/detect` в 500 именно в сценарии, ради которого try/catch и заводился (Nominatim подвис). Заменено на `catch (Exception) when (!cancellationToken.IsCancellationRequested)` — гасит любой сбой геокодера (сеть, таймаут, некорректный JSON), но пропускает исключение дальше, если отменён именно запрос вызывающего (а не внутренний таймаут HttpClient).
  - **`GeoController` больше не обращается к `CitiesController.ToDto`** — статический маппинг `CitySearchResult → CityDto` перенесён в сам `CityDto` (`CityDto.From`), оба контроллера используют его независимо друг от друга.
  - Добавлены тесты на пропущенные ранее сценарии: долгота вне диапазона (`DetectCityQueryValidator`) и передача локали в геокодер.
- **Дополнение (после фидбека фронтенда, 2026-08-23): `GET /api/cities/{cityId}`** — не было в исходной задаче; нужен, чтобы показать название сохранённого `cityId` (например, из черновика онбординга) на клиенте без повторного поиска по подстроке. `GetCityQuery`/`Handler` (`Blizka.App\UseCases\Cities`) через новый `ICityRepository.GetByIdAsync`, 404 `CITY_NOT_FOUND` (новый `CityNotFoundException`), если id не найден. Переиспользует `CityNameResolver`/`CityDto.From`, что и `GET /api/cities/search`.
- **Дополнение (по тикету QA, 2026-08-25): два бага поиска.** (1) Запрос из 1 буквы (например, `м`) не находил ничего — у однобуквенного запроса similarity против реального названия города (`≈0.11` для "м"/"Минск") оказывается ниже даже сниженного `ShortQuerySimilarityThreshold` (0.15), триграммное сходство в принципе не рассчитано на такие короткие запросы; для длины 1 `CityRepository.SearchAsync` теперь уходит в отдельный `SearchByPrefixAsync` (`ILIKE 'запрос%'`, с экранированием `%`/`_`/`\` в пользовательском вводе) вместо `TrigramsSimilarity`. (2) Точный запрос вроде `минск` проходил порог 0.15 не только для самого "Минска", но и для городов с тем же трёхграммным суффиксом ("Пинск", "Дзержинск", "Смоленск" — общее окончание "нск"), засоряя выдачу нерелевантными городами. Порог теперь зависит от длины запроса: 0.15 сохранён только для 2-3 букв (ради которых он изначально и был снижен, см. выше), с 4 букв — дефолтный GUC `pg_trgm.similarity_threshold` (0.3, `LongQuerySimilarityThreshold`). **Не проверено на живом Postgres** (в среде, где вносилась правка, не было доступного Docker) — в отличие от остального T-4.1, ручная проверка на реальном триграммном индексе ещё предстоит перед мёржем.

**Зависимости:** T-0.2.

---

### T-4.2 · Механика закрытого города и waitlist `[POST-MVP]`

**Экраны:** S-74.

**Результат:** Waitlist, счётчик, автооткрытие, уведомления.

**Что сделать:**
- Таблица `CityWaitlist` (cityId, userId, notifyOnOpen, createdAt).
- `GET /api/cities/{cityId}/status` — isOpen, waitlistCount, openThreshold, progress.
- `POST /api/cities/{cityId}/waitlist` — подписка на открытие.
- Background job `CityOpenCheck` (каждые 30 мин): waitlistCount ≥ threshold → `isOpen = true`, рассылка Telegram-уведомлений подписчикам.
- При регистрации в закрытом городе: пользователь автоматически в waitlist, может смотреть анкеты по всей Беларуси.

**Зависимости:** T-4.1, T-10.1.

---

## Эпик 5 · Лента и свайпы

### T-5.1 · Алгоритм формирования ленты `[MVP]`

**Экраны:** S-10.

**Результат:** Endpoint ленты с базовым алгоритмом подбора. ✅ Реализовано.

**Что сделать:**
- `GET /api/feed?limit=10`:
  - Выбрать кандидатов: `Status = Active`, соответствуют фильтрам пользователя (возраст, пол, расстояние, город).
  - Исключить: уже свайпнутых (join с `Swipe`), заблокированных, самого пользователя.
  - Рассчитать `compatibilityScore` для каждого кандидата.
  - Отсортировать по score, вернуть top N.
- Алгоритм совместимости (MVP — упрощённый):
  - Совпадение `datingGoal` (вес 0.15 из notes S-04).
  - Количество общих интересов / всего интересов.
  - Расстояние (ближе = лучше, но не линейно).
  - Если `isVerified` у обоих — бонус.
- Ответ: полная карточка для шторки (S-11) — интересы с `isMatch`, badges, prompts, compatibility summary.
- `exhausted: true` когда кандидаты закончились.
- PostGIS: `ST_Distance` для расчёта расстояния.

**Что сделано:**
- `GET /api/feed` (`FeedController`, `[Authorize]`, `limit` — query-параметр, дефолт 10, диапазон 1-50 через `GetFeedQueryValidator`) — `GetFeedQuery`/`Handler` (`Blizka.App\UseCases\Feed`) через новый `IFeedRepository`.
- **`UserFilter` (T-5.4) ещё не существует** (подтверждено — только комментарий-заглушка в `CompleteOnboardingCommandHandler`), поэтому кандидатов из `GetCandidatesAsync` (`FeedRepository`, `Blizka.Data\Repositories`) отбирает не персистентный фильтр, а MVP-дефолты, которые T-5.4 позже переопределит персистентными предпочтениями:
  - **Пол** — `Gender` только `Male`/`Female`, ориентация/предпочтение отдельно не хранится → показывается противоположный пол.
  - **Город** — строгое равенство `CityId` (не радиус) — "город" из списка критериев (строка 350) трактован буквально; кросс-городовый показ (в т.ч. для диаспоры, T-4.1) — вне MVP-скоупа этой задачи.
  - **Возраст** — фильтра нет вовсе (нечем: `ageRange` нигде не сохраняется до T-5.4).
  - **Заблокированные** — исключить нельзя, `UserBlock` появится только в T-16.2; в фильтре кандидатов такого условия нет.
  - Уже свайпнутые — `NOT EXISTS` по `Swipe.FromUserId`/`ToUserId` с условием `UndoneAt IS NULL` — отменённый свайп (T-5.3) возвращает кандидата в пул. Проверено вручную на реальном Postgres: вставленный `Swipe` убирал кандидата из ленты (`exhausted: true`), простановка `UndoneAt` возвращала его.
  - Кандидаты не грузятся все разом — пул ограничен константой `CandidatePoolSize = 200` (`GetFeedQueryHandler`), упорядочен по недавней активности, точный скоринг/сортировка — уже в App-слое поверх этого пула.
- **Веса скоринга — не все заданы спекой.** Вес `datingGoal` (0.15) — из заметки S-04, как и написано в задаче. Веса пересечения интересов, расстояния и бонуса за верификацию спекой не заданы — выбраны как MVP-приближение: интересы 0.35, расстояние 0.35 (сумма весов = 1.0), верификация 0.15. `FeedCompatibilityScorer` (`Blizka.App\UseCases\Feed`, internal): интересы — `общие / всего у текущего пользователя`; расстояние — не линейный спад `20 / (20 + km)` (на 20км совместимость падает вдвое); координаты неизвестны у кого-то — нейтральный вклад 0.5, а не 0 и не 1.
- **Расстояние считается гаверсинусом на C#, а не `Geometry.Distance` у NTS** — в отличие от `CityRepository.FindNearestAsync` (T-4.1), где LINQ транслируется Npgsql в PostGIS `ST_Distance` и возвращает метры, здесь сущности уже материализованы (`IReadOnlyList<User>` из репозитория) и `Point.Distance()` на in-memory геометрии посчитал бы плоское расстояние в градусах, а не метрах — тихая ошибка на порядки, если её не поймать. Задача просила `ST_Distance`/PostGIS, но для уже загрученного в память набора кандидатов (а не для запроса к БД, как в T-4.1 `FindNearestAsync`) это неприменимо.
- Источник координат для скоринга — `User.Coordinates`, а при их отсутствии (геолокация не выдана) — `City.Coordinates`; если нет и того — расстояние `null`, нейтральный вклад в скор.
- Ответ (`FeedResponse`/`FeedCardDto`, `Blizka.Api\Feed`) — карточка: фото, интересы с `isMatch`, `prompts` (как есть, `User.Prompts` — плоский `string[]`, отдельной сущности `Prompt` в домене нет), `isVerified`, `compatibilityScore` (0-100) и `compatibilitySummary` (`datingGoalMatch`, `sharedInterestsCount`, `bothVerified`) — упрощённая замена `badges` из формулировки задачи: отдельной сущности `Badge` в домене нет (grep подтвердил), а перечисленные в T-7.2 значки (`fire`/`writes_first`/`contact_opened`) — про хаб мэтча post-match, к дофсвайповой карточке неприменимы.
- Ручная проверка на реальном Postgres (`docker compose up -d postgres`, живой `dotnet run` + curl с JWT, подписанным dev-секретом): двое пользователей в одном городе, общий интерес, оба верифицированы, координаты в ~0.6км друг от друга → `compatibilityScore: 99`, `datingGoalMatch: true`, `sharedInterestsCount: 1`; `limit=999` → 400 `VALIDATION_ERROR`; без токена → 401; своп + отмена свопа — описано выше.
- Тесты: `GetFeedQueryHandlerTests` (`Blizka.UnitTests`, фейковый `IFeedRepository` — нет города/нет кандидатов → `exhausted`, дефолт пола = противоположный, сортировка по score + обрезка по `limit`, `DistanceKm: null` без координат/города, невалидный `limit` → `ValidationException`) и `FeedControllerTests` (`Blizka.IntegrationTests`, тот же минимальный тестовый хост, что и `PhotosControllerTests`).
- **Пост-ревью правки (`/code-review`), все проверены на живом Postgres, не только юнит-тестами:**
  - **`OrderByDescending(u => u.LastActiveAt)` в `FeedRepository.GetCandidatesAsync` сортировал пользователей без активности первыми, а не последними.** Postgres по умолчанию кладёт `NULL` в начало при `DESC` (Npgsql это не переопределяет) — прямо противоположно тому, что обещал doc-комментарий `IFeedRepository` ("её отсутствие — в конец"). В городе с пулом больше `CandidatePoolSize` (200) давно неактивные аккаунты вытесняли бы недавно активных кандидатов из пула ещё до скоринга. Исправлено на `OrderByDescending(u => u.LastActiveAt.HasValue).ThenByDescending(u => u.LastActiveAt)`.
  - **Добавлен `.AsSplitQuery()`** на тот же запрос — `Include(Photos)` и `Include(UserInterests).ThenInclude(Interest)` рядом без него давали декартово произведение (cartesian explosion) на каждого кандидата в пуле. `Take(poolSize)` перенесён перед `Include`, что для EF Core — обычная практика при пагинации с коллекционными `Include` (иначе лимит применился бы уже после разворачивания коллекций).
  - **`GetCurrentUserAsync` теперь тоже `AsNoTracking()`** — читается один раз только для скоринга, никогда не изменяется, но раньше EF отслеживал изменения всего графа (пользователь + `City` + `UserInterests`) без необходимости, в отличие от остальных read-only запросов в этом же файле.
  - **Разбор локали (`be`/`en`/дефолт `ru`) дублировался трижды** — в `CityLocaleParser` (T-4.1, Api-слой) и по новой копии в `GetFeedQueryHandler` (обоснование в комментарии — "App не может зависеть от Api" — было верным для направления зависимости, но не объясняло, почему сам свитч не вынесен туда, откуда его может позвать и Api, и App). Вынесено в новый `CityLocaleResolver` (`Blizka.App\UseCases\Cities`, `public`, не `internal`, как `CityNameResolver`, — ровно потому, что нужен за границей сборки) — `CityLocaleParser.Parse` и `GetFeedQueryHandler` теперь оба делегируют туда.
  - **`ResolveInterestName` вынесен в отдельный `InterestNameResolver`** (`Blizka.App\UseCases\Feed`) по образцу `CityNameResolver` — был приватным методом `GetFeedQueryHandler`, следующей фиче с локализованными названиями интересов (например, каталог интересов T-9.2) пришлось бы копировать тот же `switch` заново.
  - Не тронуто намеренно: явные проверки `candidate.City is null`/`Coordinates is null` в `FeedCompatibilityScorer` и `ToCardResult` недостижимы при текущем запросе (город кандидата всегда совпадает с городом текущего пользователя, `City.Coordinates` не `nullable`), но это осознанная подстраховка на будущее (ослабление городского фильтра в T-5.4), а не мёртвый код по ошибке — оставлено, как и было, с тем же покрытием тестами.

**Зависимости:** T-0.2, T-1.1, T-2.3.

---

### T-5.2 · Свайпы и мэтчинг `[MVP]`

**Экраны:** S-10, S-16.

**Результат:** Like/dislike, создание мэтча при взаимном лайке. ✅ Реализовано.

**Что сделать:**
- `POST /api/feed/{userId}/like` — создать `Swipe(type: Like)`. Проверить: есть ли встречный лайк → если да, создать `Match`.
- `POST /api/feed/{userId}/dislike` — создать `Swipe(type: Dislike)`.
- `POST /api/feed/{userId}/superlike` — списать зорки, создать `Swipe(type: Superlike)`, проверить мэтч.
- При мэтче (S-16): вернуть `isMatch: true` + данные мэтча + icebreakers (три входа).
- Уникальность: `(FromUserId, ToUserId)` — нельзя свайпнуть одного человека дважды.
- Транзакция: создание свайпа + проверка мэтча + (опционально списание зорок) — одна DB-транзакция.

**Зависимости:** T-5.1, T-8.1.

**Что сделано:**
- `POST /api/feed/{userId}/like|dislike|superlike` (`FeedController`, `[Authorize]`) — единая `SwipeCommand`/`SwipeCommandHandler` (`Blizka.App\UseCases\Swipes`), параметризованная `SwipeType`, через новые `ISwipeRepository`/`IMatchRepository`.
- **T-8.1 (кошелёк зорок) на момент реализации ещё не существовал** — `ISparksService` в задаче требовался только для суперлайка, поэтому заведён его минимальный срез (`Blizka.App\Sparks`): один метод `SpendAsync` поверх уже существующих `IUserRepository`/`ISparkTransactionRepository`, без `Award`/`GetBalance`/`GetHistory`/`/api/sparks/wallet` — T-8.1 достроит интерфейс, а не заменит. `SparksOptions.SuperlikeCost` — новый конфиг-раздел `Sparks` (`appsettings.yaml`), дефолт ✦5 — spec.md 15.2 сумму намеренно оставляет конфигурируемой, без своего значения, ✦5 выбран как MVP-плейсхолдер.
- Транзакционность (свайп + опциональный мэтч + опциональное списание зорок одной DB-транзакцией) — не через явный `BeginTransaction`, а тем, что все изменения (новый `Swipe`, при мэтче — новый `Match`, при суперлайке — `User.SparksBalance`/новый `SparkTransaction`) копятся в одном отслеживаемом `DbContext` и коммитятся одним `SwipeRepository.SaveChangesAsync()` — тот же паттерн, что уже применялся в T-2.3 (`CompleteOnboardingCommandHandler`).
- **Уникальность `(FromUserId, ToUserId)` пришлось пересмотреть относительно того, как индекс был заведён в T-0.2/T-5.1.** Он был обычным (не частичным) unique-индексом, а T-5.1 уже документировала и проверяла на живом Postgres, что отменённый свайп (`UndoneAt`, T-5.3) возвращает кандидата в пул ленты для повторного свайпа — без изменений повторная вставка `Swipe` той же пары после undo падала бы на этот же constraint. Индекс сделан частичным (`HasFilter("\"UndoneAt\" IS NULL")`, миграция `MakeSwipeUniqueIndexPartial`) — активных (не отменённых) свайпов пары по-прежнему не может быть больше одного, а после отмены пара свайпается заново новой строкой, история сохраняется. Проверено вручную на реальном Postgres: повторный активный свайп → `unique_violation`; тот же свайп после `UndoneAt` → проходит, обе строки (отменённая и новая) на месте; вставка `Match` — без конфликтов.
- Гонки при сохранении транслируются в клиентские ошибки, а не в 500 (по образцу `PhotoRepository`/`ConcurrentPhotoUploadException` из T-3.1): нарушение частичного индекса `Swipes` → `ConcurrentSwipeCreationException` → `AlreadySwipedException` (409, `ALREADY_SWIPED`); конкурентное списание баланса (`DbUpdateConcurrencyException` на `User`, xmin) → `ConcurrentUserUpdateException` → `SwipeConflictException` (409, `SWIPE_CONFLICT`, action `RETRY`) — без автоматического ретрая (в отличие от фото, здесь это платное действие, повторный запрос — на совести клиента). Цель свайпа не найдена → `SwipeTargetNotFoundException` (404). `InsufficientSparksException` (402) уже существовала (заведена заранее под T-8.1) и оказалась не нужна модифицировать.
- **Осознанно не решено:** при двух почти одновременных взаимных лайках (оба пользователя лайкают друг друга в одно и то же мгновение) возможно редкое окно, где оба свайпа сохранятся, но ни один не увидит другой на момент проверки и мэтч не создастся — не решается сериализуемой транзакцией/ретраем, поскольку это не задано ни decomposition.md, ни spec.md, а цена ошибки для MVP невелика (см. комментарий в `SwipeCommandHandler`).
- Icebreakers (S-16, «три лёгких входа») — не сущность БД, а фиксированный статичный набор из трёх записей (`question_of_day`/`minigame`/`date_idea`) по тексту spec.md 6.2 (`IcebreakerCatalog`), локали be/en спека не даёт — оставлены как есть, как и `Prompts` в T-5.1.
- Ответ (`SwipeResponse`/`MatchDto`/`IcebreakerDto`, `Blizka.Api\Feed`) — по форме из spec.md 6.2: `action`/`isMatch`/`match`/`sparksBalance`; `match.userId`/`match.name` — данные **другого** участника мэтча, не текущего пользователя.
- Тесты: `SwipeCommandHandlerTests` (`Blizka.UnitTests`, фейковые репозитории — цель не найдена, свайп самого себя, уже свайпнуто, лайк без/с мэтчем, дизлайк не проверяет мэтч, суперлайк при нехватке/достатке баланса) и расширенный `FeedControllerTests` (`Blizka.IntegrationTests` — мэтч со взаимным лайком и тремя icebreakers, 404 на несуществующую цель, 409 уже свайпнуто, 402 недостаточно зорок).

---

### T-5.3 · Отмена свайпа `[MVP]`

**Экраны:** S-10 (notes).

**Результат:** Undo последних 3 свайпов. ✅ Реализовано.

**Что сделать:**
- `POST /api/feed/undo`:
  - Найти последний свайп текущего пользователя с `UndoneAt IS NULL`.
  - Проставить `UndoneAt = now()`.
  - Если свайп был лайком и привёл к мэтчу — удалить мэтч (если контакт ещё не открыт).
  - Если был суперлайк — вернуть зорки.
- Счётчик: максимум 3 отмены в сутки (`UndoneAt` за последние 24 часа, count < 3).
- Валидация на сервере — клиент не может отменить больше 3.
- Возвращает `undosRemaining`.

**Зависимости:** T-5.2.

**Что сделано:**
- `POST /api/feed/undo` (`FeedController`, `[Authorize]`, без тела запроса) — `UndoSwipeCommand`/`Handler` (`Blizka.App\UseCases\Swipes`), по тому же паттерну одной DB-транзакции через `ISwipeRepository.SaveChangesAsync`, что и `SwipeCommandHandler` (T-5.2).
- Модель данных под эту задачу уже была заложена в T-5.2: `Swipe.UndoneAt` и частичный unique-индекс `(FromUserId, ToUserId) WHERE UndoneAt IS NULL`, `Match.ContactUnlockedAt`, `SparkTransactionType.Refund` — новых миграций эта задача не потребовала (`docker compose up migrator` подтвердил: `No migrations were applied`).
- **"Последний свайп" — любого типа** (`ISwipeRepository.GetLastActiveAsync`, без фильтра по `Type`), не только лайк/суперлайк: текст задачи не ограничивает набор, отмена дизлайка безвредна (просто возвращает кандидата в пул ленты T-5.1).
- **Возврат зорок за суперлайк** — новый `ISparksService.RefundAsync` (по аналогии с `SpendAsync`, T-5.2), сумма — текущая `SparksOptions.SuperlikeCost` (списание в T-5.2 тоже не хранит историческую цену, так что это симметрично); `SparkTransaction.ReferenceId` теперь указывает на `Swipe.Id` отменяемого свайпа — `Type = Refund`.
- **Мэтч удаляется физически** (`IMatchRepository.Remove` → `DbContext.Remove`), а не переводится в `MatchStatus.Archived` — в отличие от `Swipe` (там явно "проставить UndoneAt"), текст задачи говорит "удалить мэтч"; `Archived` — семантика другой будущей задачи (T-7.4, пользователь сам архивирует переписку), путать нельзя. Поиск мэтча пары — новый `IMatchRepository.GetByUsersAsync` по канонизированному `(User1Id, User2Id)` (глобально уникальная пара, см. `MatchConfiguration`).
- **Встречный свайп другого пользователя после удаления мэтча не трогается** — если пара лайкнёт друг друга снова, мэтч пересоздастся обычным путём через `SwipeCommandHandler`; текст задачи не описывает никакого кулдауна, а это поведение ожидаемо (пользователь передумал — лайк остаётся в силе).
- **"Если контакт ещё не открыт"** — реализовано буквально как условие только для удаления мэтча (`match.ContactUnlockedAt is null`): если контакт уже открыт, мэтч не трогаем, но `Swipe.UndoneAt` всё равно проставляется и зорки (если суперлайк) всё равно возвращаются — отмена самого свайпа не блокируется полностью. Сейчас это условие фактически недостижимо (T-7.3 «Открытие контакта» ещё не реализована, `ContactUnlockedAt` всегда `null`), но проверка на будущее написана сразу правильно.
- **Счётчик 3/сутки — скользящее окно 24 часа** (`ISwipeRepository.CountUndoneSinceAsync(userId, UtcNow.AddHours(-24))`), не календарные сутки — так буквально сформулировано в тексте задачи ("за последние 24 часа"), в UTC как и весь остальной код.
- Ошибки — новые `NothingToUndoException` (409, `NOTHING_TO_UNDO` — нет активного свайпа для отмены) и `UndoLimitExceededException` (422, `UNDO_LIMIT_EXCEEDED` — по аналогии с `PhotoLimitExceededException`, тоже 422 для "исчерпан числовой лимит", а не 409 как у остальных swipe-конфликтов) — оба зарегистрированы в `BlizkaExceptionHandler`/`ErrorMessageCatalog` (ru/be/en).
- Ответ (`UndoSwipeResponse`, `Blizka.Api\Feed\SwipeDtos.cs`) — сверх обязательного по тексту `undosRemaining` добавлены `action` (тип отменённого свайпа) и `userId` (кого отмена вернула в пул ленты) для симметрии с `SwipeResponse` из T-5.2 и чтобы клиенту было что показать в UI отмены.
- **Пост-ревью правка (`\code-review`):** `Match`/`Swipe` не имеют concurrency-токена (только `User.xmin`, см. `UserConfiguration`), но EF Core всё равно проверяет affected-rows на каждый UPDATE/DELETE — двойная отмена почти одновременно (или два конкурентных запроса) при удалении уже удалённого мэтча роняла `DbUpdateConcurrencyException`, которую `UndoSwipeCommandHandler` не ловил (в отличие от `SwipeCommandHandler`, T-5.2) — долетала до `BlizkaExceptionHandler` необработанной и превращалась в generic 500 вместо структурированного ApiError. Исправлено: `SaveChangesAsync` обёрнут в try/catch `ConcurrentUserUpdateException` → `SwipeConflictException` (409, `RETRY`), по образцу T-5.2. Добавлен регрессионный тест (`Handle_translates_a_concurrent_save_race_into_SwipeConflictException`).

---

### T-5.4 · Фильтры ленты `[MVP]`

**Экраны:** S-15.

**Результат:** Сохранение и применение фильтров.

**Что сделать:**
- Таблица `UserFilter` или JSON-колонка в `User`.
- `GET /api/feed/filters` — текущие фильтры.
- `PATCH /api/feed/filters` — обновить фильтры.
- Поля: `ageRange`, `maxDistanceKm`, `datingGoals`, `requireFilledProfile`, `activeWithinDays`, `requirePhoto`.
- Advanced (post-MVP toggle, но структуру заложить): `verifiedOnly`, `nonSmoker`, `nonDrinker`, `noChildren`.
- Дефолты при регистрации: из онбординга (шаг 2).
- Фильтры применяются в `GET /api/feed` на уровне SQL.

**Зависимости:** T-5.1.

**Что сделано:**
- Таблица `UserFilter` (не JSON-колонка — формулировка задачи оставляла выбор, но "фильтры применяются на уровне SQL" и переход на радиус (см. ниже) требуют типизированных/индексируемых колонок): `GET`/`PATCH /api/feed/filters` (`FeedController`, `[Authorize]`) — `GetFeedFiltersQuery`/`PatchFeedFiltersCommand` (`Blizka.App\UseCases\Feed`) через новый `IUserFilterRepository`. 1:1 с `User` по `UserId` (PK = FK, `OnDelete Cascade`, по образцу `OnboardingDraft`).
- **Согласовано с пользователем перед реализацией (три развилки, которые сама формулировка задачи оставляла открытыми):**
  - **`maxDistanceKm` заменил строгое совпадение `CityId`** (T-5.1 MVP-упрощение) **на PostGIS-радиус** — `FeedRepository.GetCandidatesAsync` фильтрует `(u.Coordinates ?? u.City!.Coordinates).Distance(origin) <= maxDistanceMeters`, тем же паттерном `.Distance()`, что и `CityRepository.FindNearestAsync` (T-4.1). Источник точки отсчёта — координаты текущего пользователя, а при их отсутствии его город (тот же fallback, что и в `FeedCompatibilityScorer`).
  - **Advanced-поля (`verifiedOnly`/`nonSmoker`/`nonDrinker`/`noChildren`) применяются в выборке сразу**, а не только "структура заложена" — в проекте нет механизма feature-флагов, разводить хранение и применение было бы лишней сложностью без пользы.
  - **Бэкафилла для уже онбордившихся до этой задачи пользователей нет** — `ShowGender`/`AgeRange` из их `OnboardingDraft.DataJson` остаются непрочитанными (как и предупреждала заметка T-2.3). `GetFeedQueryHandler` для них продолжает использовать MVP-дефолты (см. ниже) до первого собственного `PATCH`.
- **`User.HasChildren` (`bool?`) — новое поле**, добавлено в этой задаче: `noChildren` иначе нечем фильтровать (ни онбординг, ни профиль такого поля не собирают). `null` — "не указано", а не "детей нет"; фильтр отсеивает только явное `true` (`u.HasChildren != true` — и `null`, и `false` проходят), иначе скрыл бы всех кандидатов разом. Заполнять его пока негде (нет эндпоинта редактирования профиля) — поле лежит подготовленным для будущей задачи профиля.
- **Дефолты при регистрации (из шага 2 онбординга)** — `CompleteOnboardingCommandHandler` (T-2.3) теперь заводит `UserFilter` из `ShowGender`/`AgeRange`/`DatingGoals` черновика одной транзакцией с переходом в `Active` (`BuildInitialUserFilter`, новая зависимость `IUserFilterRepository` в конструкторе). `MaxDistanceKm` — MVP-дефолт (шаг 2 не собирает расстояние вовсе).
- **MVP-дефолты для пользователей без сохранённого `UserFilter`** — `UserFilterDefaults` (`Blizka.App\UseCases\Feed`, `public`, используется и `App`, и `Data`-слоем): `AgeMin=18`/`AgeMax=99`, `MaxDistanceKm=50` (приближение к типичному радиусу города — спекой не задано, как и веса скоринга в T-5.1), `ShowGender` — противоположный пол (та же логика, что была захардкожена в T-5.1 до этой задачи), `RequireFilledProfileMinCompleteness=60` (синхронизировано с первым порогом бонусов `ProfileCompleteness` из T-2.3 — "заполненный профиль" трактован как прошедший этот порог, а не просто MVP-минимум онбординга в 35%). `GET /api/feed/filters` для такого пользователя возвращает эти дефолты, а не 404/пустоту — по тому же принципу "пустое состояние", что и `GET /api/onboarding/draft`.
- **`PATCH` — частичное обновление**: поля со значением `null` в теле запроса не трогают уже сохранённое значение; `ageRange` — единственное исключение, обновляется только целиком (`{min, max}`), чтобы нельзя было рассинхронизировать `Min`/`Max` двумя раздельными вызовами. При первом `PATCH` (`UserFilter` для пользователя ещё не существует) непереданные поля берут `UserFilterDefaults`, а `ShowGender` — противоположный пол на основе текущего `User.Gender`. Гонка двух параллельных первых `PATCH` — `ConcurrentUserFilterCreationException` (по образцу `ConcurrentOnboardingDraftCreationException`, T-2.1: перехват нарушения `PK_UserFilters`, повторное наложение изменений на уже созданную конкурентом строку) — не выброшена наружу необработанной.
- **`DatingGoal[]` в Postgres — `text[]` через поэлементный `ValueConverter`** (`UserFilterConfiguration`), не нативный enum-массив: EF Core не конвертирует enum-элементы массива через `HasConversion<string>()`, как это делает для одиночных enum-колонок в этом же проекте (`User.Gender`, `User.DatingGoal` и т.д.) — добавлен `ValueComparer` (обязателен для коллекционных свойств, иначе EF не отслеживает мутации массива). Проверено на реальном Postgres: колонка `text[]`, PATCH `{"datingGoals":["casual"]}` → фильтрация `GET /api/feed` по цели знакомств отработала корректно.
- **`GetFeedQueryHandler` теперь строит `FeedCandidateFilter`** (новый параметр-объект в `Blizka.App\Domain\Repositories`, заменил четыре плоских параметра `IFeedRepository.GetCandidatesAsync`) из сохранённого `UserFilter` либо дефолтов: `ShowGenderPreference.All` → `PreferredGender: null` (пол не фильтруется), `Male`/`Female` → соответствующий `Gender`. Гвард "нет города → пустая исчерпанная лента" (T-5.1) сохранён и дополнен: "нет ни своих координат, ни города с координатами → тоже пустая исчерпанная лента" — на практике недостижимо для `Active`-пользователя (`City.Coordinates` не `nullable`), но нужно, раз радиус стал обязательным условием выборки, а не только скоринга.
- **`FeedRepository.GetCandidatesAsync` — новые SQL-фильтры** поверх существующих (не свайпнутые, `Status = Active`): радиус (см. выше), возраст через `BirthDate` (`age >= min` ⇔ `BirthDate <= today.AddYears(-min)`, `age <= max` ⇔ `BirthDate > today.AddYears(-(max+1))` — та же арифметика, что и в `CalculateAge` карточки ленты, без построения `int`-возраста на SQL-стороне), `DatingGoals.Contains(u.DatingGoal)`, `ProfileCompleteness >= 60`, `LastActiveAt >= now - N дней` (`null` исключается явно — "не заходил никогда" не проходит фильтр активности), `Photos.Any()`, `IsVerified`, `Smoking/Drinking == No` (не `null` — "гарантия" трактуется строго, неизвестное не проходит), `HasChildren != true`.
- Ручная проверка на реальном Postgres (`docker compose up -d postgres`, миграция применена, живой `dotnet run` + curl с JWT, подписанным dev-секретом, четыре вручную вставленных пользователя: свой + `NearFemale`/`FarFemale`/`NearMale` на разном расстоянии и с разным `Smoking`/`DatingGoal`): `GET /filters` без сохранённых данных → MVP-дефолты (`showGender: female`, `18-99`, `50`); `GET /feed` с дефолтным радиусом вернул только `NearFemale` (`FarFemale` ~475км и `NearMale` по полу — оба отсеяны); `PATCH {showGender: all, maxDistanceKm: 2000}` → тот же `GET /feed` вернул все три; `PATCH {nonSmoker: true}` убрал `FarFemale` (курящая); `PATCH {datingGoals: [casual]}` оставил только кандидата с этой целью; `PATCH` с `ageRange.min >= max` → 400 `VALIDATION_ERROR`; без токена → 401 на обоих эндпоинтах; строка `UserFilters` после серии `PATCH` подтверждена напрямую в БД (включая `text[]`).
- Тесты: `GetFeedQueryHandlerTests` расширен (MVP-дефолт радиуса/пола без сохранённого фильтра, сохранённый фильтр с `ShowGender=All` переопределяет дефолты, новый гвард на нерешаемые координаты, тест "нет координат" адаптирован под кандидата, а не текущего пользователя — иначе он больше не проходил бы новый гвард) и `FeedControllerTests` (`GET`/`PATCH /filters`: 401, MVP-дефолты, создание с дефолтами для непереданных полей, 400 на невалидный `ageRange`). `CompleteOnboardingCommandHandlerTests` — новый тест на создание `UserFilter` из данных шага 2.
- **Пост-ревью правки (`/code-review`):**
  - **`ActiveWithinDays` нельзя было выключить обратно через `PATCH`.** Поле — `int?`, где `null` в теле запроса и так означает "не трогать сохранённое значение" (обычная семантика partial-update в этом эндпоинте) — но у самого фильтра активности есть собственное значимое состояние "выключен" (`null` в БД), для которого просто не было способа его запросить: любой `null` в запросе читался как "не менять". Добавлен сентинел `PatchFeedFiltersCommand.ClearActiveWithinDays` (`-1`) — `PATCH {"activeWithinDays": -1}` явно сбрасывает фильтр, обычный `null` по-прежнему не трогает сохранённое значение. Валидатор принимает либо положительное число, либо ровно `-1` (не любое отрицательное — иначе опечатка вроде `-5` молча читалась бы как "выключить"). Тесты: сохранение → чтение → сброс через сентинел, и 400 на `-5`.
  - **Проверено на реальном Postgres с включённым EF Core command logging (`Serilog__MinimumLevel__Override__Microsoft.EntityFrameworkCore=Information`), не осталось ли дублирующего `JOIN` к `Cities`** — предположение было, что `COALESCE(u.Coordinates, c.Coordinates)` в `WHERE` (радиус) и `.Include(u => u.City)` в проекции могли развернуться в два отдельных `LEFT JOIN "Cities"`. По факту в сгенерированном SQL — один `LEFT JOIN "Cities" AS c`, переиспользованный и для `ST_Distance(COALESCE(...), ...)`, и для колонок `Include`. Опасение не подтвердилось, изменений не потребовалось.
  - Добавлен тест на то, что второй `PATCH`, меняющий только одно поле, не сбрасывает значения, сохранённые первым `PATCH` — партиал-апдейт для уже существующего (не только вновь создаваемого) `UserFilter` раньше не был явно закреплён тестом.
  - Уточнён устаревший doc-комментарий `IUserRepository.GetByIdAsync` — упоминал только T-5.2, хотя теперь используется и в `GetFeedFiltersQueryHandler`/`PatchFeedFiltersCommandHandler`.
  - Все 184 теста (`Blizka.UnitTests` + `Blizka.IntegrationTests`) зелёные.

---

## Эпик 6 · Симпатии

### T-6.1 · Списки лайков `[MVP]`

**Экраны:** S-21.

**Результат:** Входящие и исходящие лайки. ✅ Реализовано.

**Что сделать:**
- `GET /api/likes/incoming` — кто лайкнул меня (без мэтча). MVP: возвращает `count` и `blurredPreviews` (заблюренные фото). Полный список — после unlock.
- `GET /api/likes/outgoing` — кого лайкнул я.
- `POST /api/likes/incoming/reveal` — списать ✦10, открыть список навсегда.
- Флаг `User.LikesRevealed` (bool) — после разблокировки всегда показывать.
- Разблокировка открывает список навсегда — не за каждого отдельно.

**Зависимости:** T-5.2, T-8.1.

**Что сделано:**
- `GET /api/likes/incoming|outgoing`, `POST /api/likes/incoming/reveal` (`LikesController`, `[Authorize]`) — `GetIncomingLikesQuery`/`GetOutgoingLikesQuery`/`RevealIncomingLikesCommand` (`Blizka.App\UseCases\Likes`) через новый `ILikesRepository` (`Blizka.Data\Repositories\LikesRepository`), отдельный от `ISwipeRepository` — тот отвечает за мутации свайпов, этот только читает списки поверх `Swipe`/`Match`. Во всех выборках пара, уже образовавшая `Match` (независимо от статуса), исключается — «без мэтча» по тексту задачи для входящих и симметрично для исходящих (смэтченные показываются в мэтчах, T-7.1, не здесь) — это решение для исходящих текстом задачи прямо не оговорено, принято по аналогии.
- **T-8.1 (полный кошелёк с `Award`/`GetBalance`/`GetHistory`) на момент реализации всё ещё не существует** (как и на момент T-5.2/T-5.3) — использован тот же минимальный `ISparksService.SpendAsync` (T-5.2). Новый `SparkTransactionType.LikesReveal` (между `Superlike` и `Purchase`, по порядку из spec.md 15.2) и `SparksOptions.LikesRevealCost` (дефолт ✦10, дословно из decomposition.md) — миграции не потребовалось: `Type` хранится как `text` (`HasConversion<string>()`), новое значение enum — не изменение схемы.
- **`User.LikesRevealed` уже существовало** в модели и миграции `InitialCreate` — заведено заранее под эту задачу (T-5.х это поле не трогали). Разблокировка — `user.LikesRevealed = true` + `ISparksService.SpendAsync`, сохранение одной транзакцией через `IUserRepository.SaveChangesAsync` (тот же приём, что и `swipeRepository.SaveChangesAsync()` в T-5.2/T-5.3 — общий отслеживаемый `DbContext`, разные фасады-репозитории).
- **`POST /reveal` идемпотентен**: если `LikesRevealed` уже `true`, зорки повторно не списываются (`sparksSpent: 0`), возвращается только актуальный список — текст задачи говорит «открывает список навсегда, не за каждого отдельно», из чего последовательно следует, что повторный вызов не должен списывать повторно. Гонка двух параллельных первых `reveal` — `LikesRevealConflictException` (409, `RETRY`) поверх `ConcurrentUserUpdateException`, по образцу `SwipeConflictException` (T-5.2).
- **Заблюренные превью (`blurredPhotoUrl`, spec.md 7.1) — согласовано с пользователем перед реализацией**: в кодовой базе не было никакого механизма блюра фото (T-3.1 генерирует только оригинал/thumbnail/medium). Выбран блюр на лету по запросу: `GetIncomingLikesQueryHandler` скачивает уже сохранённый thumbnail через новый `IPhotoStorageService.DownloadAsync(key)` (реализация — `s3Client.GetObjectAsync`, ключ собирается из `PhotoStorageKeys.Thumbnail(Prefix(userId, photoId))`, без похода в БД за самим ключом), размывает через новый `PhotoImageProcessor.Blur` (`GaussianBlur` с сигмой 20 — на уже небольшом 150px thumbnail этого достаточно, чтобы лицо не читалось) и отдаёт как `data:image/jpeg;base64,...` прямо в JSON-ответе. Ничего не кэшируется и не хранится отдельным вариантом фото в T-3.1 — цена генерации на лету ограничена (превью — не более 4 фото за запрос), а сохранённый вариант потребовал бы миграции и бэкафилла для уже загруженных до этой задачи фото.
- Превью (`GetIncomingPreviewAsync`) ограничено четырьмя записями — по примеру ответа в spec.md 7.1 (ровно четыре `blurredPhotoUrl`). Лайкнувший без главного фото в превью пропускается, а не падает с ошибкой.
- Общая проекция `LikeEntry → LikeUserResult` (userId/name/age/mainPhotoUrl) вынесена в `LikeResultMapper` — используется всеми тремя use case'ами (включая полный список после разблокировки и ответ `POST /reveal`).
- Ручная проверка на реальном Postgres + MinIO (`docker compose up -d postgres`, миграция подтвердила «No migrations were applied», `docker compose up -d minio minio-init`, живой `dotnet run` + curl с JWT, подписанным dev-секретом; четыре вручную вставленных лайкнувших, один из которых уже смэтчен со мной, у одного залит реальный thumbnail в MinIO): `GET /incoming` без разблокировки → `count: 2` (смэтченная Cleo корректно исключена), `preview` — один элемент (у второй лайкнувшей нет фото — пропущена), `blurredPhotoUrl` — валидный data-URI; `GET /outgoing` → мой единственный исходящий лайк; `POST /reveal` → `sparksSpent: 10`, баланс списан, `LikesRevealed` в БД стал `true`, `SparkTransactions` — новая строка `Type = LikesReveal`, `Amount = -10`; повторный `POST /reveal` → `sparksSpent: 0`, баланс не изменился; `POST /reveal` с нулевым балансом → 402 `INSUFFICIENT_SPARKS`; без токена → 401.
- Тесты: `GetIncomingLikesQueryHandlerTests`, `GetOutgoingLikesQueryHandlerTests`, `RevealIncomingLikesCommandHandlerTests` (`Blizka.UnitTests`, фейковые репозитории — блюр превью, пропуск лайкнувших без фото, разблокированное состояние, недостаточно зорок, идемпотентность повторного reveal, гонка конкурентного сохранения) и `LikesControllerTests` (`Blizka.IntegrationTests`, тот же минимальный тестовый хост, что и `FeedControllerTests`: 401 на все три эндпоинта, заблюренное превью до разблокировки, полный список после, 402 на недостаточный баланс, идемпотентный повторный `reveal`). `PhotoImageProcessorTests` — новый тест на `Blur` (валидный JPEG тех же размеров).
- **Пост-ревью правка (`/code-review`):** `BlurMainPhotosAsync` скачивал/размывал thumbnail без обработки ошибок — один недоступный или повреждённый объект в S3/MinIO (несогласованность с БД, гонка с удалением фото между чтением списка и генерацией превью) обрушивал весь `GET /incoming` 500-й вместо того, чтобы просто пропустить эту запись, как уже делалось при отсутствии главного фото. Обёрнуто в `try/catch (Exception) when (!cancellationToken.IsCancellationRequested)` — отмена запроса по-прежнему пробрасывается штатно, а любая другая ошибка скачивания/декодирования одной записи просто пропускает её. Тест: `Handle_skips_a_preview_entry_when_the_download_fails`.
- **Найдено и исправлено (тикет ClickUp):** `IncomingQuery`/`OutgoingQuery` (`LikesRepository`) не исключали удалённые аккаунты (`User.Status = Deleted`, soft delete по T-16.2) — лайкнувший/лайкнутый, удаливший аккаунт после свайпа, оставался в списке навсегда, в т.ч. в платном разблокированном `GET /incoming`. Добавлен фильтр `FromUser.Status != Deleted`/`ToUser.Status != Deleted` в обе выборки. Лента (T-5.1, `FeedRepository.GetCandidatesAsync`) уже фильтрует `Status == Active`, так что удалённых кандидатов в ней не бывает — правки не потребовалось. Мэтчи (`MatchRepository`, T-7.1) такого фильтра не имеют — вопрос, должен ли удалённый партнёр пропадать из уже существующего мэтча (а не только из ещё не смэтченных списков лайков), тикетом прямо не задан и не решён в этой правке. Прямого теста на EF-запрос `LikesRepository` нет — в проекте по-прежнему нет инфраструктуры интеграционных тестов на реальном Postgres (см. T-7.4).

---

## Эпик 7 · Мэтчи и хаб

### T-7.1 · Список мэтчей `[MVP]`

**Экраны:** S-30.

**Результат:** Три секции мэтчей: новые, ждут сообщения, архив. ✅ Реализовано.

**Что сделать:**
- `GET /api/matches`:
  - `new` — `Status = Active`, `ContactUnlockedAt IS NULL`.
  - `waitingForMessage` — `ContactUnlockedAt IS NOT NULL`, нет подтверждения отправки.
  - `archived` — `Status = Archived`.
- Бейджи: `fire` (высокий score), `writes_first` (настройка приватности партнёра), `contact_opened`.
- Сортировка: новые — по `matchedAt` DESC, ждут — по `contactUnlockedAt` DESC.

**Зависимости:** T-5.2.

**Что сделано:**
- `GET /api/matches` (`MatchesController`, `[Authorize]`) — `GetMatchesQuery`/`GetMatchesQueryHandler` (`Blizka.App\UseCases\Matches`) поверх трёх новых методов `IMatchRepository` (`GetNewAsync`/`GetWaitingForMessageAsync`/`GetArchivedAsync`, `Blizka.Data\Repositories\MatchRepository`) — ровно условия из задачи (`waitingForMessage` дополнительно фильтрует `Status = Active`, чтобы уже заархивированный по T-7.4 мэтч не показывался тут повторно). Обе стороны `Match` (`User1`/`User2`) грузятся целиком (`Photos`, `UserInterests.Interest`, `City`, `AsSplitQuery` — по тому же соображению, что и в `FeedRepository`) — нужны и для проекции второго участника, и для скоринга совместимости.
- **Два пункта уточнены с пользователем перед реализацией, спекой не заданы:** порог бейджа `fire` («высокий score») — `score ≥ 80` по шкале `FeedCompatibilityScorer` (0-100, переиспользован как есть, `internal` в той же сборке); `writesFirst`/бейдж `writes_first` зависят от настройки приватности партнёра «Запретить писать мне в Telegram» (T-16.1, ещё не реализована, `PrivacySettings` в коде нет) — по решению пользователя всегда `false` до появления T-16.1, по аналогии с MVP-заглушками недостающих веток в T-7.2.
- **`Match.ArchivedAt` (новое поле + миграция `T7_1_MatchArchivedAt`)** — понадобилось для `archivedAt` в ответе секции `archived`; T-7.4 (сама архивация) ещё не реализована и ничего это поле пока не проставляет, поэтому в маппинге фоллбэк на `MatchedAt`, если `ArchivedAt` не задан. `reason` — единственное описанное в spec.md 8.1 значение `"no_activity_7_days"`, захардкожено (второй сценарий архивации из decomposition.md T-7.4 — контакт открыт, но нет message-sent-check > 7 дней — отдельным значением спекой не размечен).
- `SparksOptions.ContactUnlockCost` (дефолт ✦1, дословно из decomposition.md T-7.3) — добавлено сейчас, хотя списание (T-7.3) ещё не реализовано, т.к. `contactCost` уже нужен в ответе `GET /api/matches`.
- Общая проекция `Match → MatchUserResult` (второй участник) и резолвинг «я / второй участник» относительно канонизированной пары `User1`/`User2` — в `MatchResultMapper`, переиспользуется всеми тремя секциями.
- Ручная проверка на реальном Postgres (`docker compose up -d postgres`, миграция применена, временный тест напрямую через `MatchRepository` на живой БД — подтвердил, что `AsSplitQuery` с двойным `Include` по обеим сторонам мэтча и `OrderByDescending(m => m.ArchivedAt ?? m.MatchedAt)` транслируются в SQL корректно; тестовые данные удалены после проверки).
- Тесты: `GetMatchesQueryHandlerTests` (`Blizka.UnitTests`, фейковый `IMatchRepository` — бейдж `fire` при высокой совместимости, отсутствие бейджа при низкой, резолвинг второго участника независимо от того, кто из пары `User1`/`User2` — текущий пользователь, `contact_opened` с `contactOpenedAt = ContactUnlockedAt`, фоллбэк `archivedAt` на `MatchedAt`) и `MatchesControllerTests` (`Blizka.IntegrationTests`, тот же минимальный тестовый хост, что и `LikesControllerTests`: 401 без токена, все три секции разом с бейджами). Все 249 тестов (`Blizka.UnitTests` + `Blizka.IntegrationTests`) зелёные.

---

### T-7.2 · Хаб мэтча `[MVP]`

**Экраны:** S-31.

**Результат:** Детальная карточка мэтча со статусами всех фич. ✅ Реализовано.

**Что сделать:**
- `GET /api/matches/{matchId}`:
  - Данные пользователя: имя, возраст, город, lastActive, mainPhoto.
  - `telegramUsername` — только если контакт разблокирован.
  - `compatibility` — score + текстовое описание совпадений.
  - `contactStatus`: `locked` | `unlocked` | `writes_first_only`.
  - `features`: статус каждой ветки (questionOfDay, minigame, dateIdea, staleConversation) — MVP: только `contactStatus`, остальные `available: false`.
- Проверка доступа: пользователь — участник мэтча.

**Зависимости:** T-7.1.

**Что сделано:**
- `GET /api/matches/{matchId}` (`MatchesController.GetMatchHub`, `[Authorize]`) — `GetMatchHubQuery`/`GetMatchHubQueryHandler` (`Blizka.App\UseCases\Matches`) поверх нового `IMatchRepository.GetByIdForUserAsync(matchId, userId)` (`Blizka.Data\Repositories\MatchRepository`, тот же `WithUsers`-инклюд, что и у T-7.1: `Photos`, `UserInterests.Interest`, `City`, `AsSplitQuery`). Точка входа ищет мэтч сразу в паре (matchId, userId) — участник ли текущий пользователь — а не отдельной проверкой после загрузки: чужой мэтч неотличим от несуществующего (`MatchNotFoundException` → 404 `MATCH_NOT_FOUND`, тот же IDOR-приём, что `PhotoNotFoundException` в T-3.1).
- `contactStatus`: `locked`/`unlocked` — по `Match.ContactUnlockedAt` (поле на уровне мэтча, симметрично для обеих сторон, разблокировка не привязана к тому, кто платил). `writes_first_only` (S-51, зависит от `PrivacySettings`/T-16.1, которой ещё нет в коде) — недостижим, тем же MVP-приёмом, что `WritesFirst: false` в T-7.1. `telegramUsername` отдаётся только при `unlocked` (spec.md 8.2).
- **`compatibility.details` — согласовано с пользователем перед реализацией, шаблон не задан ни decomposition.md, ни spec.md (там только пример готовой фразы):** текст собирается в `MatchCompatibilityDescriber` из уже посчитанных `FeedCompatibilityScorer`-факторов — перечисление совпавших интересов (`SharedInterestIds`, те же, что дают бейдж `fire` в T-7.1) плюс отдельные фразы про совпадение цели знакомства и обоюдную верификацию; если ничего не совпало — фиксированный фоллбэк «Пока мало общих данных для сравнения.».
- `features` — плоская MVP-заглушка `{ "available": false }` для всех четырёх веток (questionOfDay/minigame/dateIdea/staleConversation), как прямо требует decomposition.md, а не более богатая форма из примера в spec.md 8.2 (та относится к ещё нереализованным T-11.1/T-14.1/T-12.1/T-15.1).
- `MatchResultMapper.ToHubUserResult` — второй метод-проекция пользователя в том же мэппере, что и T-7.1 (`ToUserResult`): добавляет город (`CityNameResolver` по локали текущего пользователя, как в Feed T-5.1), `lastActive` и условный `telegramUsername`.
- Ручная проверка на реальном Postgres (`docker compose up -d postgres`, миграций не потребовалось; временный тест через `WebApplicationFactory<Program>` с реальным `BlizkaDbContext` и JWT, подписанным dev-секретом — два вручную вставленных пользователя и мэтч между ними; `GET /api/matches/{matchId}` → 200, `AsSplitQuery` с тем же двойным `Include`, что в T-7.1, корректно транслируется при фильтре по `Id` вместо `OrderBy`; `contactStatus: "locked"`, `telegramUsername: null`, все `features.*.available: false`; тестовые данные и временный тест удалены после проверки).
- Тесты: `GetMatchHubQueryHandlerTests` (`Blizka.UnitTests`, фейковый `IMatchRepository` — locked/unlocked и видимость `telegramUsername`, текст `details` по общим интересам и цели, заглушка всех четырёх `features`, `MatchNotFoundException` для чужого/несуществующего мэтча) и `MatchesControllerTests` (`Blizka.IntegrationTests`, тот же минимальный тестовый хост, что и у T-7.1: 404 на чужой/несуществующий мэтч, locked/unlocked через HTTP). Все 257 тестов (`Blizka.UnitTests` + `Blizka.IntegrationTests`) зелёные.

---

### T-7.3 · Открытие контакта (оплата зорками) `[MVP]`

**Экраны:** S-32, S-36.

**Результат:** Списание зорки, выдача Telegram username. ✅ Реализовано.

**Что сделать:**
- `POST /api/matches/{matchId}/unlock`:
  - Проверить баланс ≥ 1 (или 0, если подписка Безлимит).
  - Проверить: контакт ещё не открыт.
  - Проверить: приватность мэтча — если `blockIncomingMessages`, вернуть ошибку «Этот человек пишет первым сам».
  - Списать ✦1 → `SparkTransaction(ContactUnlock)`.
  - Обновить `Match.ContactUnlockedBy`, `Match.ContactUnlockedAt`.
  - Вернуть `telegramUsername`, `deepLink`.
- `POST /api/matches/{matchId}/message-sent-check` — фронт вызывает после возврата из Telegram. Метрика, аналитика.

**Зависимости:** T-7.2, T-8.1.

**Что сделано:**
- `POST /api/matches/{matchId}/unlock` — `MatchesController.UnlockContact` (`src/Blizka.Api/Controllers/MatchesController.cs`), обработчик `UnlockContactCommandHandler` (`src/Blizka.App/UseCases/Matches/UnlockContactCommandHandler.cs`). Списание ✦1 (`SparksOptions.ContactUnlockCost`, уже заведён в T-7.2) через `ISparksService.SpendAsync(..., SparkTransactionType.ContactUnlock, ...)`, обновление `Match.ContactUnlockedAt`/`ContactUnlockedByUserId`, ответ — `telegramUsername`, `deepLink` (`https://t.me/{username}`), `sparksSpent`, `sparksBalance`.
- **Идемпотентность вместо 409 «уже открыт»** — согласовано с пользователем при уточнении задачи: повторный вызов (тем же пользователем или вторым участником мэтча) просто возвращает уже доступный контакт без повторного списания, `sparksSpent: 0`.
- **Конкурентность** — на `Match` добавлен `xmin`-токен (`MatchConfiguration`, миграция `T7_3_MatchXminConcurrencyToken`) против гонки, когда оба участника почти одновременно жмут «Открыть контакт»; конфликт сохранения превращается в `ContactUnlockConflictException` → HTTP 409 (`CONTACT_UNLOCK_CONFLICT`).
- **Проверка `blockIncomingMessages` пропущена** — таблицы `PrivacySettings` нет в коде (T-16.1 не реализована), тот же MVP-приём, что и `writesFirst` в T-7.1/T-7.2 (согласовано с пользователем).
- **Точка расширения под T-8.3** (подписка «Безлимит» → бесплатный unlock) — `ISubscriptionChecker.HasUnlimitedContactUnlocksAsync`, тот же интерфейс, что и дневной лимит свайпов в `SwipeCommandHandler`; реализация нигде не регистрируется в DI, пока T-8.3 не сделана, поэтому конструктор принимает её как опциональный параметр (`null` по умолчанию) и стоимость списывается всегда.
- `POST /api/matches/{matchId}/message-sent-check` — `MessageSentCheckCommandHandler` (`src/Blizka.App/UseCases/Matches/MessageSentCheckCommandHandler.cs`), 204 No Content. Проставляет `Match.MessageSentCheckAt` идемпотентно, один раз: повторные вызовы не сдвигают момент — иначе окно архивации T-7.4 («нет message-sent-check более 7 дней после открытия контакта») продлевалось бы бесконечно от одного и того же нажатия.
- Тесты: `UnlockContactCommandHandlerTests`, `MessageSentCheckCommandHandlerTests` (`tests/Blizka.UnitTests/UseCases/Matches/`), плюс контроллерные тесты в `MatchesControllerTests`.

---

### T-7.4 · Архивация мэтчей `[MVP]`

**Экраны:** S-30 (notes).

**Результат:** Автоматическая и ручная архивация. ✅ Реализовано.

**Что сделать:**
- Background job `ArchiveStaleMatches` (каждые 6 часов):
  - Мэтчи с `Status = Active`, `ContactUnlockedAt IS NULL`, `MatchedAt` > 7 дней назад → `Status = Archived`.
  - Мэтчи с контактом, но без `message-sent-check` > 7 дней → `Status = Archived`.
- `POST /api/matches/{matchId}/archive` — ручная архивация.
- `DELETE /api/matches/{matchId}/archive` — вернуть из архива (бесплатно, всегда).

**Зависимости:** T-7.1.

**Что сделано:**
- Условие протухания вынесено в `MatchArchivalPolicy` (`Blizka.App\UseCases\Matches`) — общий порог `StaleAfter = 7 дней` и `IsStale(...)` для уже загруженных в память сущностей (используется в `GetMatchesQueryHandler` для эвристики `Reason`). В `IMatchRepository.ArchiveStaleMatchesAsync` (реализация — `MatchRepository`, `Blizka.Data`) то же условие продублировано как LINQ-предикат для `ExecuteUpdateAsync` — вызов произвольного C#-метода внутри `Where` для `ExecuteUpdateAsync` EF Core в SQL не транслирует.
- `ArchiveStaleMatchesJob` (`Blizka.Host\Jobs`, первая реальная Quartz-джоба в проекте — список был пуст с T-0.1) регистрируется прямо в `Program.cs` (`AddQuartz(q => ...)` с `AddJob`/`AddTrigger`, `WithSimpleSchedule().WithIntervalInHours(6).RepeatForever()`) — массовая архивация одним `ExecuteUpdateAsync`, без построчной загрузки сущностей и без прохода через `SaveChangesAsync` (побочных эффектов вроде списаний/уведомлений у этой операции нет).
- `POST /api/matches/{matchId}/archive` (`ArchiveMatchCommand`/`Handler`) и `DELETE /api/matches/{matchId}/archive` (`UnarchiveMatchCommand`/`Handler`) — тот же паттерн IDOR-защиты и идемпотентности, что и у T-7.3 (`GetByIdForUserTrackedAsync`, повторный вызов на уже архивном/уже активном мэтче не пишет в БД повторно). `DELETE` доступен без ограничения по числу вызовов и без списания зорок — «бесплатно, всегда» из текста задачи.
- **Восстановленный мэтч не защищён от повторной автоархивации** — `DELETE /archive` не трогает `MatchedAt`/`ContactUnlockedAt`, на которых завязано условие протухания, поэтому если пользователь восстановил протухший мэтч и ничего не сделал, следующий прогон джобы (до 6 часов) заархивирует его снова. Согласовано с пользователем при уточнении задачи — специальной отсрочки/снятия с автоархивации не вводится, «бесплатно, всегда» относится только к отсутствию стоимости и лимита попыток восстановления.
- **`ArchivedMatchResult.Reason` различает два значения** вместо единственной заглушки T-7.1 (`"no_activity_7_days"`): `MatchArchivalPolicy.AutoArchivedReason`/`ManualArchivedReason` (`"no_activity_7_days"`/`"manual"`, второе значение спекой не размечено). Проставляется **в момент архивации** — новое поле `Match.ArchivedReason` (миграция `T7_4_MatchArchivedReasonAndActiveIndex`), а не эвристикой на момент чтения. Эвристика (`MatchArchivalPolicy.IsStale`) осталась в `GetMatchesQueryHandler` только фолбэком для мэтчей без `ArchivedReason` (легаси-данные/фикстуры).
  - **Найдено и исправлено в code review**: первая версия вычисляла `Reason` эвристически при каждом чтении — мэтч, заархивированный вручную на 1-й день (`Reason = "manual"`), после того как реально проходило 7 дней с `MatchedAt`, задним числом переквалифицировался бы в `"no_activity_7_days"`, хотя джоба его не трогала. Регрессия закрыта тестом `Handle_returns_the_persisted_reason_verbatim`.
- Та же миграция добавляет частичный индекс `IX_Matches_Status` (`WHERE "Status" = 'Active'`) — тоже находка ревью: без него предикат `ArchiveStaleMatchesAsync` был бы full scan `Matches` на каждый 6-часовой прогон джобы.
- `ArchiveStaleMatchesJob` помечена `[DisallowConcurrentExecution]` — предикат и так идемпотентен (`Status = Active` исключает повторную обработку), но это первая джоба в проекте и стоит сразу задать паттерн на случай, если выполнение когда-нибудь превысит 6-часовой интервал триггера.
- Тесты: `MatchArchivalPolicyTests`, `ArchiveMatchCommandHandlerTests`, `UnarchiveMatchCommandHandlerTests` (`tests/Blizka.UnitTests/UseCases/Matches/`), плюс контроллерные тесты в `MatchesControllerTests` и два кейса персистентной/фолбэк-причины в `GetMatchesQueryHandlerTests`. Бесперебойность самого `ExecuteUpdateAsync`-предиката джобы отдельным DB-интеграционным тестом не покрыта — как и остальные EF-запросы `MatchRepository` (`GetNewAsync`/`GetWaitingForMessageAsync` и т.д.), в проекте нет инфраструктуры интеграционных тестов на реальном Postgres.

---

## Эпик 8 · Экономика зорок

### T-8.1 · Кошелёк и транзакции `[MVP]`

**Экраны:** S-46, S-07.

**Результат:** Баланс, начисления, списания, история. ✅ Реализовано.

**Что сделать:**
- `User.SparksBalance` — денормализованное поле, обновляется атомарно.
- `SparkTransaction` — лог всех операций.
- Сервис `ISparksService`:
  - `Award(userId, amount, type, referenceId)` — начисление.
  - `Spend(userId, amount, type, referenceId)` — списание. Кидает `InsufficientSparksException` если баланса не хватает.
  - `GetBalance(userId)`.
  - `GetHistory(userId, page, pageSize)`.
- Транзакционность: `UPDATE users SET sparks_balance = sparks_balance - @amount WHERE id = @id AND sparks_balance >= @amount` — атомарно, без race condition.
- `GET /api/sparks/wallet` — баланс + earn options + история.
- Таблица начислений (из spec раздел 15.2): registration 50, profile 2+2+2, verification 3, referral 2, idea 1/10.

**Что сделано:**
- Достроен уже существовавший минимальный срез `ISparksService` (`SpendAsync`/`RefundAsync` из T-5.2/T-5.3) до полного контракта: `AwardAsync`, `GetBalanceAsync`, `GetHistoryAsync` (`Blizka.App\Sparks\SparksService`) — не замена, а расширение, как и предполагали заметки T-5.2/T-6.1/T-7.3. `GetBalanceAsync` добавил `SparksService` новую зависимость `IUserRepository` (раньше сервису хватало только `ISparkTransactionRepository`) — потребовало явно зарегистрировать `IUserRepository` в тестовом хосте `MatchesControllerTests` (раньше не требовался, `UnlockContactCommandHandler` резолвит обе стороны мэтча прямо из `Match.User1/User2`).
- **Атомарность списания реализована не буквально** (не raw `UPDATE ... WHERE sparks_balance >= @amount`), а через уже принятую в T-5.2/T-7.3 модель: whole-row optimistic concurrency на `xmin` (`UserConfiguration`) → `DbUpdateConcurrencyException` → `ConcurrentUserUpdateException` → фичевые 409. `AwardAsync`, как и уже существовавший `AwardAsync` в онбординге, этой защитой не оборачивается — начисления одному пользователю параллельно не гонятся, сохранение (и обработка конфликта) остаются на совести вызывающего хендлера.
- **Онбординг (T-2.3) отрефакторен** — приватный `CompleteOnboardingCommandHandler.AwardAsync`, писавший `SparksBalance`/`SparkTransaction` напрямую в обход `ISparksService` (заведён до появления интерфейса), заменён на вызовы `ISparksService.AwardAsync`, чтобы `Award` остался единственным источником правды. `ISparkTransactionRepository` в конструкторе хендлера заменён на `ISparksService` + `IOptions<SparksOptions>`.
- **Суммы начислений перенесены в `SparksOptions`/`appsettings.yaml`** (по аналогии с уже конфигурируемыми `SuperlikeCost`/`LikesRevealCost`/`ContactUnlockCost`): `RegistrationBonusAmount` (50, был `private const` в онбординге), `ProfileCompletionThresholdBonusAmount` (2, был `ProfileCompletenessCalculator.ThresholdBonusSparks`, из-за чего `ProfileCompletenessCalculator.NextReward` стал принимать сумму параметром вместо константы), плюс `VerificationBonusAmount`/`ReferralBonusAmount`/`IdeaSubmissionBonusAmount`/`IdeaImplementedBonusAmount` — без вызывающего кода на момент реализации (появится в T-18.1/T-20.1/T-19.1 соответственно), заведены заранее под таблицу начислений и earn-options кошелька. Заодно в `appsettings.yaml` явно прописан `ContactUnlockCost: 1` — раньше держался только на дефолте класса.
- **`GET /api/sparks/wallet`** (`SparksController`, `Blizka.App\UseCases\Sparks\GetSparksWalletQuery`) — баланс + пагинированная история (страница через уже существовавший, но нигде не задействованный `PaginatedResponse<T>`; `page`/`pageSize` по конвенции `GetFeedQueryValidator`, диапазон `pageSize` 1-50, дефолты 1/20 — MVP-плейсхолдер, спекой не заданы) + статический каталог `earnOptions` (тип начисления → сумма из `SparksOptions`, без персонализированных флагов «уже получено» — для них нет ни поля, ни данных вне онбординга).
- `ISparkTransactionRepository.GetHistoryAsync` — новый метод чтения (`Skip`/`Take` + `CountAsync`, сортировка `CreatedAt` убыв.); реализован в `SparkTransactionRepository`.
- **Осознанно не исправлено:** `SparkTransactionRepository.SaveChangesAsync` по-прежнему не перехватывает `DbUpdateConcurrencyException` (в отличие от `UserRepository`/`MatchRepository`) — не стало проблемой, потому что ни `Award`/`Spend`/`Refund`, ни новый `GetHistoryAsync` не вызывают этот `SaveChangesAsync` напрямую: сохранение всегда идёт через репозиторий, владеющий изменённым `User` (тот же `DbContext`).

**Зависимости:** T-0.2, T-1.1.

---

### T-8.2 · Покупка зорок за Telegram Stars `[POST-MVP]`

**Экраны:** S-75.

**Результат:** Интеграция с Telegram Payments.

**Что сделать:**
- `POST /api/sparks/purchase` — создать invoice через Bot API `createInvoiceLink`.
- Пакеты: 20✦/99⭐, 50✦/229⭐, 120✦/499⭐ (конфиг, не хардкод).
- `POST /api/webhooks/telegram-stars` — обработка `successful_payment`:
  - Верификация `X-Telegram-Bot-Api-Secret-Token`.
  - Идемпотентность по `telegram_payment_charge_id`.
  - Начисление зорок через `ISparksService.Award`.
- Обработка `refunded_payment` — списание, если баланс позволяет.
- Таблица `TelegramPayment` — лог платежей.

**Зависимости:** T-8.1, T-10.1.

---

### T-8.3 · Подписка «Безлимит» `[POST-MVP]`

**Экраны:** S-76.

**Результат:** Месячная подписка через Telegram Stars.

**Что сделать:**
- Таблица `Subscription`.
- `GET /api/subscriptions/me` — статус, фичи, дата следующего списания.
- `POST /api/subscriptions/unlimited/activate` — создать invoice (399⭐/мес).
- `POST /api/subscriptions/unlimited/cancel` — отмена, доступ до конца периода.
- Background job `SubscriptionRenewal` — проверка истекших подписок.
- Влияние на логику: контакт unlock бесплатный, свайпы безлимитные, 5 суперлайков/неделю, режим невидимки.
- Middleware или сервис `ISubscriptionChecker` — проверка активной подписки при списании зорок.

**Зависимости:** T-8.2.

---

## Эпик 9 · Профиль

### T-9.1 · Просмотр и редактирование профиля `[MVP]`

**Экраны:** S-40.

**Результат:** CRUD профиля, расчёт completeness. ✅ Реализовано.

**Что сделать:**
- `GET /api/users/me` — полные данные профиля + баланс зорок + completeness + nextReward.
- `PATCH /api/users/me/profile` — частичное обновление: name, bio, height, smoking, drinking, chronotype, prompts, datingGoal.
- При каждом обновлении: пересчёт `ProfileCompleteness`.
- Проверка порогов: если completeness впервые достигла 60%/80%/100% — начислить бонусные зорки.
- `GET /api/users/me/preview` — профиль в формате карточки ленты (как видят другие).
- Валидация: name 1–30 символов, prompts max 3 штуки × max 200 символов.

**Что сделано:**
- `GetMeQuery`/`GetMeQueryHandler`/`UserMeResponse` расширены на месте (не задублированы вторым эндпоинтом), как предписывала более ранняя заметка этой задачи: теперь возвращают gender/birthDate/cityId/bio/height/smoking/drinking/chronotype/prompts/datingGoal/isVerified/instagramHandle/voiceIntroUrl вдобавок к уже бывшим id/telegramId/name/sparksBalance/status/locale, плюс `profileCompleteness` и `nextReward` — тот же `NextProfileReward`/`NextRewardHintCatalog`, что и у `POST /api/onboarding/complete` (T-2.3), локаль резолвится тем же `RequestLocaleResolver`, а не персистентной `User.Locale`. `ProfileCompleteness` на GET считается "по требованию" через уже готовый `ProfileCompletenessCalculator` (T-2.3), без побочных начислений — пороговые бонусы начисляются только при фактическом изменении профиля.
- `PATCH /api/users/me/profile` (`PatchUserProfileCommand`+`Handler`+`Validator`) — частичное обновление ровно по списку полей выше (city/gender/birthDate туда не входят — переносятся один раз при завершении онбординга, T-2.3; Instagram/голосовое приветствие — предмет отдельных будущих задач). Семантика "`null` — не менять" — по образцу `PatchFeedFiltersCommand` (T-5.4): следствие в том, что через этот эндпоинт нельзя вернуть `height`/`smoking`/`drinking`/`chronotype` обратно в `null`, только `bio`/`prompts` можно очистить, прислав пустую строку/пустой массив — сочтено приемлемым, отдельного сентинела под них не заводилось.
- После патча профиль пересчитывает `ProfileCompleteness` и начисляет бонус за впервые достигнутый порог тем же общим `ProfileCompletenessBonusAwarder` (новый, вынесен из `CompleteOnboardingCommandHandler`, T-2.3, чтобы не дублировать защиту от повторного начисления через `CompletenessBonus60/80/100AwardedAt` в двух местах).
- **Найдено на код-ревью, исправлено:** `PatchUserProfileCommandHandler` изначально не ловил `ConcurrentUserUpdateException` вокруг `SaveChangesAsync` — в отличие от всех остальных хендлеров, сохраняющих `User` (`CompleteOnboardingCommandHandler` и др.), которые переигрывают xmin-конфликт в доменное исключение с 409. Без этого два параллельных `PATCH /api/users/me/profile` одного пользователя роняли бы проигравший запрос в необработанный 500. Добавлены `ProfileUpdateConflictException` (по образцу `LikesRevealConflictException`) и его маппинг в `BlizkaExceptionHandler` на 409/`RETRY`.
- `GET /api/users/me/preview` (`GetProfilePreviewQuery`+`Handler`) — тот же набор полей, что и карточка ленты (`FeedCardResult`, T-5.1: имя, возраст, bio, город, фото, интересы, промпты, верификация, цель), без полей, которые не имеют смысла для собственного профиля (расстояние, совместимость) — переиспользует `CityLocaleResolver`/`CityNameResolver`/`InterestNameResolver` из T-5.1.
- `IUserRepository.GetByIdWithProfileDataAsync` дополнен `.Include(City)` и `.ThenInclude(Interest)` для `UserInterests` (раньше грузил только сами связи без каталожных данных) — нужно для имени города/интересов в новых ответах; T-2.3 и остальные вызывающие коды не пострадали, просто получают чуть больше данных в той же загрузке.
- **Валидация полей, явно не заданных decomposition.md** — рост (`height`) ограничен диапазоном 100–250 см, bio — 500 символами; оба значения выбраны как разумное приближение, а не взяты из спеки.
- Тесты: `GetMeQueryHandlerTests`, `PatchUserProfileCommandHandlerTests`, `GetProfilePreviewQueryHandlerTests` (`Blizka.UnitTests`) и расширенные `UsersControllerTests` (`Blizka.IntegrationTests`) — включая частичное обновление, идемпотентность порогового бонуса и 400 VALIDATION_ERROR на слишком длинное имя.
- **Пост-релизная правка (тикет ClickUp):** `GET /api/users/{userId}` — анкета произвольного пользователя, тот же набор полей, что и `GET /api/users/me/preview` выше. Понадобился, потому что списки лайков (T-6.1, `LikeUserDto`) отдают только `userId`/`name`/`age`/`mainPhotoUrl` — тапнуть на человека в списке и посмотреть полную анкету было неоткуда, даже если его уже нет в ленте. Новый `UserProfilesController` (`api/users`, отдельно от `UsersController` — тот занят исключительно `api/users/me`), `GetUserProfileQuery`/`Handler` по образцу `GetProfilePreviewQueryHandler`. Удалённый аккаунт (`Status = Deleted`) недоступен по прямой ссылке — `UserProfileNotFoundException` → 404 `USER_PROFILE_NOT_FOUND`, та же причина, по которой удалённые пользователи теперь исключены из списков лайков (см. T-6.1). Тесты: `GetUserProfileQueryHandlerTests` (`Blizka.UnitTests`), `UserProfilesControllerTests` (`Blizka.IntegrationTests`).

**Зависимости:** T-0.2, T-1.1, T-8.1.

---

### T-9.2 · Интересы `[MVP]`

**Экраны:** S-43.

**Результат:** Каталог интересов, выбор пользователем. ✅ Реализовано.

**Что сделать:**
- `GET /api/interests/catalog?locale=ru` — полный каталог по категориям.
- `PATCH /api/users/me/interests` — `{ interestIds: [...] }`.
- Пользовательские интересы: если `interestId` не найден в каталоге и `isCustom: true` — создать новый.
- Поиск по каталогу: `GET /api/interests/search?q=скал`.
- Пересчёт `ProfileCompleteness` после обновления.

**Что сделано:**
- `GET /api/interests/catalog?locale=ru` (`InterestsController`, `[Authorize]`) — полный каталог, сгруппированный по `InterestCategory`; `GET /api/interests/search?q=...&locale=ru` — trigram-поиск (pg_trgm), не более 10 результатов, по образцу `CitiesController`/`CityRepository` (T-4.1), включая тот же GIN-индекс `gin_trgm_ops` на `NameRu/NameBe/NameEn` (миграция `T9_2_InterestIndexes`).
- `PATCH /api/users/me/interests` (`UsersController`) — задаёт **полный** набор интересов пользователя (замена, как `prompts` в T-9.1), пересчитывает `ProfileCompleteness` и начисляет пороговый бонус тем же `ProfileCompletenessBonusAwarder`, что и T-9.1.
- **Контракт запроса расходится с буквальным `{ interestIds: [...] }` из decomposition.md** — decomposition требует уметь создавать кастомный интерес "если `interestId` не найден в каталоге и `isCustom: true`", но без готового id создать такую запись одним лишь `interestIds` невозможно (backend-spec.md, откуда мог бы быть точный контракт, в репозитории нет). Решение: тело `{ interestIds: Guid[], customInterests: string[] }` — `interestIds` выбирают уже существующие интересы каталога, `customInterests` — названия новых.
- **Кастомные интересы общие для всех пользователей, без модерации** (подтверждено пользователем) — созданный `Interest{ IsCustom: true }` сразу попадает в общий каталог/поиск наравне с предустановленными. Дубликаты по названию не создаются: перед созданием ищется существующий интерес с тем же `NameRu` без учёта регистра (`IInterestRepository.FindByNameAsync`, поиск через `ILike` с экранированием спецсимволов шаблона, как в `SearchByPrefixAsync`) — если найден, переиспользуется его id. На случай гонки (два параллельных `PATCH` создают один и тот же новый кастомный интерес одновременно) на `Interest.NameRu` добавлен отдельный **обычный уникальный B-tree индекс** `IX_Interests_NameRu_Unique` (отдельно от GIN-индекса для поиска — GIN не поддерживает `UNIQUE`); нарушение индекса ловится в `UserRepository.SaveChangesAsync` и транслируется в `InterestCreationConflictException` (409 `INTEREST_CREATION_CONFLICT`, action `RETRY`) — клиент просто повторяет запрос, вторая попытка находит уже созданный интерес через `FindByNameAsync`. Индекс регистронезависимые дубликаты (разный регистр одного названия) не ловит — остаточный риск признан приемлемым для MVP.
- Перевод кастомных интересов на be/en недоступен — хранятся под одним и тем же названием на всех трёх локалях (`NameRu = NameBe = NameEn`).
- Кастомным интересам присвоена отдельная категория `InterestCategory.Custom` (новое значение enum) — decomposition.md не описывает, к какой категории они относятся, а каталог группируется по категориям.
- **Лимит в 20 интересов на пользователя** (каталожных и кастомных суммарно, подтверждено пользователем) — decomposition.md лимита не задаёт; проверяется `PatchUserInterestsCommandValidator` (400 `VALIDATION_ERROR`), не отдельным доменным исключением, так как это чистая проверка формы запроса (как `prompts` max 3 в T-9.1), а не состояния БД.
- Несуществующий `interestId` в `PATCH` → `InterestNotFoundException` (404 `INTEREST_NOT_FOUND`); параллельный `PATCH` того же пользователя → переиспользован `ProfileUpdateConflictException` (409, тот же принцип, что и в T-9.1); гонка при создании кастомного интереса → `InterestCreationConflictException` (409, см. выше).
- Тесты: `GetInterestCatalogQueryHandlerTests`, `SearchInterestsQueryHandlerTests`, `PatchUserInterestsCommandHandlerTests` (`Blizka.UnitTests`) — включая замену полного набора, создание/переиспользование кастомного интереса, лимит, пороговый бонус и оба вида конфликта конкурентного сохранения. Интеграционные тесты и применение миграции `T9_2_InterestIndexes` к реальной БД не проверялись в этой сессии — Docker недоступен в среде разработки.

**Зависимости:** T-9.1.

---

### T-9.3 · Предпочтения на свидания `[POST-MVP]`

**Экраны:** S-42.

**Результат:** Выбор предпочтений, использование в алгоритме. ✅ Реализовано (кроме использования в «Идее свидания», T-12.1 — сама фича ещё не реализована).

**Что сделать:**
- Каталог предпочтений: `active_outdoors`, `calm_hangout`, `quizzes_board_games`, `something_new`.
- `PATCH /api/users/me/date-preferences` — `{ preferences: [...] }`.
- Учёт в алгоритме подбора (T-5.1) — совпадение предпочтений → бонус к score.
- Использование в «Идее свидания» (T-12.1).

**Что сделано:**
- Каталог из 4 значений уже существовал (сеяится в T-0.2, `DatePreferenceSeed`) — сущности `DatePreference`/`UserDatePreference` тоже были, но `User` не имел навигационной коллекции к `UserDatePreference`; добавлена `User.UserDatePreferences`, `UserDatePreferenceConfiguration` теперь ссылается на неё через `.WithMany(u => u.UserDatePreferences)` (чисто модельное изменение, миграция не потребовалась — `dotnet ef migrations has-pending-model-changes` подтвердил отсутствие изменений схемы).
- `IUserDatePreferenceRepository.GetCatalogAsync` — полный каталог (4 значения) для PATCH и `GET /api/date-preferences/catalog`.
- `PATCH /api/users/me/date-preferences` (`Blizka.Api.Controllers.UsersController.PatchDatePreferences`, `PatchUserDatePreferencesCommand(+Handler+Validator)`) — по образцу `PatchUserInterestsCommandHandler` (T-9.2): полная замена набора (а не добавление/удаление), пересчёт `ProfileCompleteness` (уже поддерживал бонус `DatePreferencesBonus=10` за `datePreferenceCount > 0`, T-2.3) и начисление порогового бонуса через тот же `ProfileCompletenessBonusAwarder`, `ConcurrentUserUpdateException` → `ProfileUpdateConflictException` (409). В отличие от интересов, каталог фиксированный (закрытый `enum DatePreferenceCode`) — не нужна логика создания новых записей/уникальных имён, только фильтрация запрошенных кодов по каталогу.
- `GET /api/date-preferences/catalog` (`DatePreferencesController`, `GetDatePreferenceCatalogQuery(+Handler)`) — по образцу `GET /api/interests/catalog`.
- Учёт в скоринге ленты (T-5.1, `FeedCompatibilityScorer`): добавлен вес `DatePreferencesWeight = 0.10` (доля пересечения предпочтений, как `InterestsWeight` для интересов), веса `InterestsWeight`/`DistanceWeight` уменьшены с 0.35 до 0.30 каждый, чтобы сумма весов осталась 1.0 (конкретные числа — не из спеки, решение по аналогии с остальными весами T-5.1). `GetFeedQueryHandler`, `GetMatchesQueryHandler` (бейдж `fire`) и `GetMatchHubQueryHandler` (карточка совместимости мэтча) обновлены — передают набор Id предпочтений текущего пользователя. `FeedRepository`/`MatchRepository` подгружают `UserDatePreferences` через `.Include(...)` для обеих сторон. В ответ ленты (`FeedCardResult`/`FeedCompatibilitySummaryDto`) добавлено поле `SharedDatePreferencesCount` — по аналогии с `SharedInterestsCount`.
- Использование в «Идее свидания» не реализовано — T-12.1 сама ещё не реализована (см. её раздел).

**Зависимости:** T-9.1.

---

## Эпик 10 · Telegram-интеграция

### T-10.1 · Telegram Bot API сервис `[MVP]` ✅ Реализовано

**Результат:** Инфраструктурный сервис для отправки сообщений и создания invoice.

**Что сделать:**
- `ITelegramBotService`:
  - `SendMessage(telegramId, text, parseMode)` — отправка уведомлений.
  - `CreateInvoiceLink(...)` — для покупки зорок и подписки.
  - `GetUserProfilePhotos(telegramId)` — для импорта аватара.
- HttpClient + retry policy (Polly).
- Rate limiting: Telegram допускает ~30 msg/sec в бота.
- Конфиг: `BotToken`, `WebhookSecret`, `PaymentProviderToken`.

**Что сделано:**
- `ITelegramBotService` (`Blizka.App/Domain/Services/ITelegramBotService.cs`) — `SendMessageAsync`, `CreateInvoiceLinkAsync`, `GetUserProfilePhotosAsync`; реализация `TelegramBotService` (`Blizka.Data/Telegram/`), по тому же паттерну, что `INominatimGeocoder`/`ITelegramAvatarDownloader` (интерфейс в App, импл + опции в Data, typed `HttpClient` через `AddHttpClient` в `DataServiceCollectionExtensions.AddDataLayer`).
- `TelegramOptions` (`Blizka.Data/Telegram/TelegramOptions.cs`) биндится на уже существующую секцию `Telegram` (`BotToken`/`WebhookSecret`/`PaymentProviderToken` были в `appsettings.yaml` с T-0.1, но не потреблялись до этой задачи). `BotToken` намеренно **не** проверяется на непустоту через `ValidateOnStart` — в локальной разработке он пуст (см. `DevLogin:Secret` в `TelegramAuthMiddleware`, T-1.1), и обязательная проверка сломала бы старт хоста локально.
- `HttpClient` для сервиса конфигурируется с базовым адресом `https://api.telegram.org/bot{BotToken}/` — методы вызывают его относительными путями (`sendMessage`, `createInvoiceLink`, `getUserProfilePhotos`, `getFile`). Штатные логирующие HTTP-хендлеры (`Microsoft.Extensions.Http`) для этого клиента явно отключены через `.RemoveAllLoggers()` — иначе они пишут полный URI каждого запроса на уровне Information, а токен бота — часть URI (того требует Bot API: `/bot{token}/{method}`), и утёк бы в логи (Serilog) целиком, открывая полный контроль над ботом.
- Retry — сырой Polly v8 `ResiliencePipeline<HttpResponseMessage>` (без пакета `Microsoft.Extensions.Http.Resilience`, которого нет в решении) прямо в `TelegramBotService`: до 3 повторов с экспоненциальной задержкой на `HttpRequestException`/`TaskCanceledException` (сетевые сбои) **и** на HTTP 429/5xx от самого Telegram (реальный статус-код, не только `ok:false` в теле) — именно 429 и есть ожидаемый транзиентный случай при рассылке уведомлений (T-10.2), клиентский лимитер (см. ниже) снижает его частоту, но не исключает целиком. Не устроившие `ShouldHandle` ответы явно `Dispose()`-ятся в `OnRetry`, чтобы не утекали при повторной попытке. Ответы с `ok:false` без транзиентного статус-кода (неверный `chat_id`, невалидный invoice и т.п.) не ретраятся — `TelegramApiException` сразу наружу. Пакет `Polly` (уже был в `Directory.Packages.props`, использовался только `Blizka.Host` — добавлена `PackageReference` в `Blizka.Data.csproj`).
- Rate limiting — `System.Threading.RateLimiting.FixedWindowRateLimiter`, 30 запросов/сек с очередью на 100 (`DataServiceCollectionExtensions`). Зарегистрирован как **keyed** singleton (`AddKeyedSingleton<RateLimiter>("telegram", ...)`), а не обычный `AddSingleton<RateLimiter>`, как у `INominatimGeocoder` (1 запрос/сек) — при двух незакеенных регистрациях одного типа DI отдаёт только последнюю, что тихо сломало бы лимитер Nominatim. Если очередь лимитера переполнена — не ждать бесконечно, а сразу `TelegramApiException`.
- `TelegramApiException` (`Blizka.App/Domain/Exceptions/TelegramApiException.cs`) — **не** наследует `BlizkaDomainException`: это ещё не клиентская ошибка с оформившимся контрактом (как её показать пользователю — решает T-10.2/T-8.2, которые появятся позже), поэтому не заведена в `ErrorMessageCatalog`.
- `CreateInvoiceLinkAsync` поддерживает как оплату Telegram Stars (`currency: "XTR"`, `providerToken` пустой — T-8.2/T-8.3), так и фиатную валюту: если вызывающий код не передал `ProviderToken` для валюты, отличной от `"XTR"`, сервис сам подставляет сконфигурированный `Telegram:PaymentProviderToken`, чтобы вызывающий код не мог случайно отправить фиатный invoice с пустым токеном провайдера.
- `GetUserProfilePhotosAsync` — двухшаговый вызов Bot API (`getUserProfilePhotos` → берём самый крупный размер каждой фотографии → `getFile` на каждый `file_id`), возвращает прямые `https://api.telegram.org/file/bot{token}/{file_path}` ссылки; они одноразово-временные (Telegram Bot API отдаёт `file_path`, действительный ограниченное время) — скачивать сразу, не сохранять как постоянный URL. Пока не подключён ни к одному контроллеру — предполагаемый потребитель (импорт аватара) уже покрыт отдельным `ITelegramAvatarDownloader`/`ImportTelegramPhotoCommandHandler` (T-3.1) через URL с Telegram Login Widget; этот метод для будущих сценариев, где такого URL нет.
- Юнит-тесты (`Blizka.UnitTests/Telegram/TelegramBotServiceTests.cs`) — на стабовом `HttpMessageHandler`, без реального Bot API: успешные вызовы, `ok:false` → исключение, retry на сетевой ошибке и на 429 (включая исчерпание всех попыток), отказ при заполненной очереди rate-лимитера, разбор `getUserProfilePhotos`/`getFile`, fallback `PaymentProviderToken` для фиатной валюты (и его отсутствие для Stars).
- Ревью (code-review skill, MCP Roslyn-анализ) выявило и устранило критическую утечку токена бота в логи (см. про `.RemoveAllLoggers()` выше) и отсутствие retry на 429/5xx — оба фикса вошли в этот же коммит.

**Зависимости:** T-0.1.

---

### T-10.2 · Уведомления `[MVP]` ✅ Реализовано

**Результат:** Отправка Telegram-уведомлений по событиям.

**Что сделать:**
- `INotificationService` с методами по типам событий:
  - `NotifyMatch(userId, matchName)` — «У вас новый мэтч!».
  - `NotifyNewProfiles(userId)` — «Появились новые анкеты».
  - `NotifyCityOpen(userIds, cityName)` — «Мы запустились в {город}!».
- Очередь уведомлений (Quartz job или Channel + BackgroundService).
- Локализация: текст на языке получателя.
- `GET /api/notifications/unread` — количество непрочитанных (likes, matches).
- Не отправлять, если пользователь на паузе.

**Что сделано:**
- `INotificationService`/`NotificationService` (`Blizka.App/Notifications/`) — все три метода по образцу `ISparksService`/`SparksService` (интерфейс и реализация в App, без EF/HTTP-зависимостей). Каждый метод не шлёт сообщение сам, а кладёт `PendingNotification` (UserId, `NotificationType`, необязательный `Placeholder` — имя мэтча/название города) в `INotificationQueue`; `NotifyCityOpenAsync` разворачивает список получателей в отдельные записи по одному на пользователя.
- Очередь — **Channel + BackgroundService**, не Quartz-джоба: события (мэтч, открытие города) рождаются в момент действия, а не по расписанию, опрос по таймеру добавил бы только задержку доставки. `INotificationQueue`/`NotificationQueue` (`Blizka.App/Notifications/`) — обёртка над `Channel.CreateUnbounded`, singleton (переживает scoped HTTP-запрос, в котором уведомление поставлено). Читатель — `NotificationDispatchBackgroundService` (`Blizka.Host/BackgroundServices/`), `AddHostedService` в `Program.cs`: на каждое уведомление открывает свой DI-scope (репозитории и `ITelegramBotService` — scoped/через `AddHttpClient`), резолвит пользователя, проверяет паузу и локаль, отправляет через `ITelegramBotService.SendMessageAsync`. Сбой отдельной отправки (пользователь удалён, Telegram отклонил чат) логируется (`ILogger.LogWarning`) и не прерывает цикл чтения очереди — иначе одна протухшая запись остановила бы уведомления для всех.
- Очередь — не персистентная (in-memory `Channel`): рестарт хоста теряет ещё не отправленные уведомления. Для MVP это осознанный компромисс — `decomposition.md` не задаёт требования пережить рестарт, а `Notification`-таблицы (лог/персистентная очередь) в домене нет и не появится раньше T-20.1 (см. `CLAUDE.md`).
- Локализация — `NotificationMessageCatalog` (`Blizka.App/Notifications/`) со статическим словарём шаблонов `NotificationType → CityLocale → string`, по образцу `ErrorMessageCatalog` (`Blizka.Api`), но на локали **пользователя** (`CityLocaleResolver.Resolve(user.Locale)`), а не запроса — уведомление шлётся фоново, вне HTTP-контекста, `Accept-Language`/JWT-claim недоступны.
- «Не отправлять, если пользователь на паузе» — отдельного поля `IsPaused` в домене нет, это `User.Status == UserStatus.Paused`; проверка — в `NotificationDispatchBackgroundService` перед отправкой (пользователь также молча пропускается, если успел удалиться).
- `GET /api/notifications/unread` (`NotificationsController`, `GetUnreadNotificationsCountQuery(+Handler)`) — `likes` берётся как есть из `ILikesRepository.CountIncomingAsync` (T-6.1). Для `matches` в домене нет отдельного флага/таймстампа «прочитано» (`decomposition.md` не задаёт его и для T-10.2) — вместо новой сущности/миграции переиспользовано условие секции «new» из T-7.1 (`Status = Active`, контакт ещё не открыт) как естественная граница непрочитанного — открытие контакта уже само по себе выводит мэтч из «new». Не через `GetNewAsync` напрямую — тот тянет `Photos`/`UserInterests`/`UserDatePreferences`/`City` обеих сторон каждого мэтча (нужны для проекции и скоринга совместимости в T-7.1), а бейджу нужно только количество; добавлен отдельный лёгкий `IMatchRepository.CountNewAsync` (тот же фильтр, без `Include`/`AsSplitQuery`) — иначе часто опрашиваемый клиентом бейдж на каждый запрос гонял бы полный граф профилей ради `.Count`.
- Вызов `NotifyMatchAsync` подключён в `SwipeCommandHandler` (T-5.2) — при взаимном лайке уведомляются оба участника, после успешного `SaveChangesAsync` (чтобы не уведомить о мэтче, который в итоге не сохранился из-за `ConcurrentUserUpdateException`). `INotificationService` — необязательный (`= null`) конструкторный параметр, по образцу уже существующего `ISubscriptionChecker?` в этом же хендлере, чтобы не ломать не связанные с уведомлениями тесты. Источников для `NotifyNewProfilesAsync`/`NotifyCityOpenAsync` в реализованных задачах ещё нет (T-4.2 waitlist-город и T-9.x «новые анкеты» — либо post-MVP, либо не описывают конкретный триггер) — методы готовы, но пока не вызываются; это ожидаемо по графу зависимостей `decomposition.md` (T-4.2 зависит от T-10.1, не от T-10.2, и появится позже).

**Зависимости:** T-10.1.

---

## Эпик 11 · Вопрос дня

### T-11.1 · Вопрос дня `[POST-MVP]`

**Экраны:** S-37.

**Результат:** Ежедневный вопрос для пар, обмен ответами.

**Что сделать:**
- Таблица `QuestionOfDay` (id, textRu, textBe, textEn, publishedAt).
- Таблица `QuestionAnswer` (questionId, userId, matchId, text, answeredAt).
- Background job `GenerateQuestionOfDay` (ежедневно 18:50) — выбрать или сгенерировать вопрос, опубликовать в 19:00.
- `GET /api/matches/{matchId}/question-of-day` — текущий вопрос, мой ответ, ответ партнёра (null если не оба ответили).
- `POST /api/matches/{matchId}/question-of-day/answer`.
- При ответе обоих: уведомление через Telegram.
- Архив: `GET /api/matches/{matchId}/questions/archive?page=1`.

**Зависимости:** T-7.2, T-10.2.

---

## Эпик 12 · Идея свидания

### T-12.1 · Генерация идей свидания `[POST-MVP]`

**Экраны:** S-39.

**Результат:** AI-генерация идей на основе общих предпочтений.

**Что сделать:**
- `GET /api/matches/{matchId}/date-ideas?city=Минск&maxBudget=30&currency=BYN`.
- Логика: найти пересечение `datePreferences` и `interests` обоих пользователей.
- Генерация 2–3 идей через LLM с контекстом: город, бюджет, общие интересы/предпочтения.
- Каждая идея: title, description, estimatedCost, estimatedDuration, inviteText.
- `POST /api/matches/{matchId}/date-confirmed` — зафиксировать договорённость о встрече.
- Background job `PostDateSurvey` — push опрос через 24 часа после `date-confirmed`.

**Зависимости:** T-7.2, T-9.3, T-13.1.

---

## Эпик 13 · AI-генерация сообщений

### T-13.1 · AI-сервис генерации сообщений `[POST-MVP]`

**Экраны:** S-34, S-35.

**Результат:** Генерация первого сообщения на основе анкеты собеседника.

**Что сделать:**
- `POST /api/ai/generate-message`:
  - Вход: `matchId`, `anchors` (до 2 деталей из анкеты), `tone`.
  - Выход: 3 варианта текста + `remainingAttempts` + `modifiers`.
- `IAiMessageService`:
  - Сбор контекста: интересы собеседника, промпты, общие интересы, цель.
  - System prompt с жёсткими правилами: 2–4 предложения, опора на конкретную деталь, открытый вопрос в конце, **никаких комплиментов внешности**.
  - Модификаторы: `shorter`, `funnier`, `no_question`, `warmer`.
- Лимит: 5 генераций на мэтч (счётчик в `Match` или отдельная таблица).
- Внешний LLM: OpenAI / Anthropic через HttpClient.
- `POST /api/ai/generate-message/modify` — повторная генерация с модификатором.

**Зависимости:** T-7.2.

---

## Эпик 14 · Мини-игра

### T-14.1 · Мини-игра «20 дилемм» `[POST-MVP]`

**Экраны:** S-38.

**Результат:** Игра для пары, подсчёт совпадений.

**Что сделать:**
- Каталог дилемм (seed): ~50 пар, чтобы при повторной игре были новые.
- `GET /api/matches/{matchId}/minigame` — создать игру, вернуть 20 случайных дилемм.
- `POST /api/matches/{matchId}/minigame/answers` — сохранить ответы.
- `GET /api/matches/{matchId}/minigame/result` — подсчёт совпадений, выбор 3 «тем для спора», генерация `shareText`.
- Результат доступен только когда ответили оба.

**Зависимости:** T-7.2.

---

## Эпик 15 · «Диалог заглох»

### T-15.1 · Детекция и генерация тем `[POST-MVP]`

**Экраны:** S-41.

**Результат:** Автоматическое предложение тем через 2 дня тишины.

**Что сделать:**
- Background job `DetectStaleConversations` (каждые 4 часа):
  - Мэтчи с `ContactUnlockedAt` > 2 дней назад, `message-sent-check` не подтверждён, `StaleNotifiedAt IS NULL`.
  - Генерация 3 тем через LLM (контекст: общие интересы, промпты, предпочтения).
  - Сохранение тем, установка `StaleNotifiedAt`.
- `GET /api/matches/{matchId}/stale-topics` — 3 темы для перезапуска.
- Появляется один раз — повторно не напоминает.

**Зависимости:** T-7.2, T-13.1.

---

## Эпик 16 · Приватность и безопасность

### T-16.1 · Настройки приватности `[MVP]`

**Экраны:** S-51.

**Результат:** Управление видимостью данных.

**Что сделать:**
- Таблица `PrivacySettings` или JSON-колонка в `User`.
- `GET /api/privacy/settings`.
- `PATCH /api/privacy/settings`:
  - `blockIncomingMessages` — username не показывается, «пишет первой сама».
  - `hideDistance` — виден только город.
  - `hideAge` — возраст скрыт.
  - `showLastActive` — «был(а) недавно».
  - `invisibleMode` — только для подписчиков Безлимит (проверка).
- Влияние на `GET /api/feed`: `hideDistance` → `distanceKm: null`, `hideAge` → `age: null`.
- Влияние на `GET /api/matches/{matchId}`: `blockIncomingMessages` → `contactStatus: writes_first_only`.

**Зависимости:** T-1.1.

---

### T-16.2 · Блокировка и управление аккаунтом `[MVP]`

**Экраны:** S-51.

**Результат:** Блокировка пользователей, пауза, удаление аккаунта. 🟡 Реализовано частично — только удаление аккаунта.

**Что сделать:**
- Таблица `UserBlock` (blockerId, blockedId, createdAt).
- `POST /api/users/{userId}/block`, `DELETE /api/users/{userId}/block`.
- `GET /api/users/me/blocked` — список заблокированных.
- Блокировка влияет на ленту: заблокированный не появляется, не может лайкать.
- `POST /api/users/me/pause` — `Status = Paused`, скрыт из ленты, мэтчи сохраняются.
- `POST /api/users/me/resume` — `Status = Active`.
- `DELETE /api/users/me/account` — `Status = Deleted`, `DeletedAt = now()`. Soft delete 30 дней.
- `GET /api/users/me/data-export` — background job, формирует JSON-архив, отправляет ссылку в Telegram.

**Что сделано:**
- Реализован только `DELETE /api/users/me/account` — понадобился фронту как единственный способ дать пользователю
  «полный сброс» аккаунта (до этого метода не существовало вовсе, а `Status`/`DeletedAt` у `User` уже были заведены
  в T-0.2 про запас). `UserBlock`, `pause`/`resume`, `data-export` — не реализованы, остаются в этой задаче.
- `DeleteAccountCommand`/`Handler` (`Blizka.App\UseCases\Users`) — `Status = Deleted`, `DeletedAt`/`UpdatedAt = now()`.
  Идемпотентно по образцу T-7.3 (`UnlockContactCommandHandler`): повторный вызов на уже удалённом аккаунте не бросает
  ошибку и не трогает БД повторно, а просто возвращает 204 — тот же приём, что и в T-7.3, согласован с пользователем.
- Физическая очистка через 30 дней после `DeletedAt` — не реализована (нет background job для неё; в отличие от
  `ArchiveStaleMatches` из T-7.4 эта задача не заводит такую джобу явно, а просто описывает soft-delete как факт).
- Не проверялось, блокирует ли статус `Deleted` дальнейшие запросы по уже выданному JWT — аутентификация (T-1.1)
  не перепроверяет `User.Status` в БД на каждый запрос, только валидность токена. Не в скоупе этого изменения.
- Тесты: `DeleteAccountCommandHandlerTests` (`tests/Blizka.UnitTests/UseCases/Users/`) + `DeleteAccount_*`
  в `UsersControllerTests` (`tests/Blizka.IntegrationTests/Controllers/`), включая проверку идемпотентности.

**Зависимости:** T-1.1, T-10.1.

---

## Эпик 17 · Жалобы и модерация

### T-17.1 · Жалобы от пользователей `[MVP]`

**Экраны:** S-13.

**Результат:** Подача жалоб, автоматический shadowban.

**Что сделать:**
- `POST /api/users/{userId}/report` — `{ reason, comment, blockUser }`.
- 7 типов жалоб (из S-13) с маппингом приоритетов:
  - `underage`, `unsafe_meeting` → Critical (немедленная блокировка + ручная проверка).
  - `scam`, `explicit` → High.
  - `fake_photos`, `insults`, `spam` → Normal.
- `blockUser: true` → одновременно `POST /api/users/{userId}/block`.
- Background job `ShadowbanAutoCheck` (каждые 2 часа):
  - 3+ жалоб за 24 часа на одного пользователя → `Status = Shadowbanned`.
  - Shadowbanned: профиль не показывается в ленте, пользователь не знает об этом.

**Зависимости:** T-16.2.

---

### T-17.2 · Admin API для модерации `[POST-MVP]`

**Результат:** Интерфейс для модераторов.

**Что сделать:**
- `GET /api/admin/reports?status=pending&priority=critical&page=1` — очередь жалоб.
- `POST /api/admin/reports/{reportId}/resolve` — `{ action: warn|shadowban|ban|dismiss, reason }`.
- `POST /api/admin/users/{userId}/ban` — `{ reason, durationDays }`.
- `POST /api/admin/users/{userId}/unban`.
- `GET /api/admin/users/{userId}` — полная информация о пользователе для модератора.
- Admin-аутентификация: отдельный JWT с role claim `admin`.
- SLA: «Проверим в течение 12 часов» — дашборд для отслеживания.

**Зависимости:** T-17.1.

---

## Эпик 18 · Верификация

### T-18.1 · Верификация по селфи `[POST-MVP]`

**Экраны:** S-49.

**Результат:** Face matching между селфи и загруженными фото.

**Что сделать:**
- `POST /api/verification/selfie` — загрузка селфи.
- Pipeline:
  - Face detection на селфи.
  - Face embedding → сравнение с embeddings загруженных фото.
  - Similarity > threshold → `Verified`.
- `GET /api/verification/status` — `pending | verified | rejected`.
- При верификации: +✦3, бейдж «Проверен», `User.IsVerified = true`.
- ML: face_recognition library или внешний API (AWS Rekognition, Azure Face).

**Зависимости:** T-3.1, T-8.1.

---

## Эпик 19 · Доска идей

### T-19.1 · Доска идей и голосования `[POST-MVP]`

**Экраны:** S-60.

**Результат:** Community-фича для предложений.

**Что сделать:**
- `GET /api/ideas?sort=hot|new&page=1`.
- `POST /api/ideas` — `{ text, anonymous }`. Зорки: +✦1 раз в месяц.
- `POST /api/ideas/{ideaId}/vote`, `DELETE /api/ideas/{ideaId}/vote`.
- Статусы: `new → under_review → planned → implemented | declined`.
- При `implemented`: +✦10 автору + бейдж «Соавтор».
- Admin endpoint: `PATCH /api/admin/ideas/{ideaId}/status`.

**Зависимости:** T-8.1.

---

## Эпик 20 · Реферальная система

### T-20.1 · Реферальные ссылки `[MVP]`

**Экраны:** S-47.

**Результат:** Генерация ссылок, трекинг, начисление бонусов.

**Что сделать:**
- Таблица `Referral` (referrerId, referredId, code, status, createdAt).
- `POST /api/referrals/invite` — генерация deep link `https://t.me/blizka_bot?start=ref_{code}` + shareText.
- При онбординге: если `start` параметр содержит `ref_` — записать `referrerId`.
- При завершении онбординга реферала: начислить +✦2 рефереру.
- `GET /api/referrals/stats` — invited, registered, sparksEarned.

**Зависимости:** T-8.1, T-2.3.

---

## Эпик 21 · Безопасность свидания

### T-21.1 · Центр безопасности `[POST-MVP]`

**Экраны:** S-54.

**Результат:** Шаринг плана свидания.

**Что сделать:**
- `POST /api/safety/share-date-plan` — `{ matchId, place, dateTime }`.
- Генерация текста для отправки другу: место, время, ссылка на анкету мэтча.
- Статический контент (признаки мошенника, правила безопасности) — на фронте, не требует backend endpoint.

**Зависимости:** T-7.2.

---

## Сводная таблица

### MVP-задачи (Phase 1)

| # | Задача | Эпик | Зависимости | Оценка |
|---|--------|------|-------------|--------|
| T-0.1 | Инициализация проекта | Фундамент | — | S |
| T-0.2 | Доменные сущности + EF Core | Фундамент | T-0.1 | M |
| T-0.3 | Формат ответов, ошибки | Фундамент | T-0.1 | S |
| T-1.1 | Telegram auth middleware | Auth | T-0.1, T-0.2 | M |
| T-2.1 | Черновик онбординга | Онбординг | T-1.1 | M |
| T-2.2 | Согласие пользователя | Онбординг | T-1.1 | S |
| T-2.3 | Завершение онбординга | Онбординг | T-2.1, T-2.2, T-3.1, T-8.1 | M |
| T-3.1 | Загрузка и управление фото | Фото | T-0.2, T-1.1 | M |
| T-4.1 | Поиск городов | Города | T-0.2 | S |
| T-5.1 | Алгоритм ленты | Лента | T-0.2, T-1.1, T-2.3 | L |
| T-5.2 | Свайпы и мэтчинг | Лента | T-5.1, T-8.1 | M |
| T-5.3 | Отмена свайпа | Лента | T-5.2 | S |
| T-5.4 | Фильтры ленты | Лента | T-5.1 | S |
| T-6.1 | Списки лайков | Симпатии | T-5.2, T-8.1 | M |
| T-7.1 | Список мэтчей | Мэтчи | T-5.2 | S |
| T-7.2 | Хаб мэтча | Мэтчи | T-7.1 | M |
| T-7.3 | Открытие контакта | Мэтчи | T-7.2, T-8.1 | M |
| T-7.4 | Архивация мэтчей | Мэтчи | T-7.1 | S |
| T-8.1 | Кошелёк зорок | Экономика | T-0.2, T-1.1 | M |
| T-9.1 | Просмотр/редактирование профиля | Профиль | T-0.2, T-1.1, T-8.1 | M |
| T-9.2 | Интересы | Профиль | T-9.1 | S |
| T-10.1 | Telegram Bot API сервис | Telegram | T-0.1 | M |
| T-10.2 | Уведомления | Telegram | T-10.1 | M |
| T-16.1 | Настройки приватности | Приватность | T-1.1 | S |
| T-16.2 | Блокировка, пауза, удаление | Приватность | T-1.1, T-10.1 | M |
| T-17.1 | Жалобы + auto-shadowban | Модерация | T-16.2 | M |
| T-20.1 | Реферальные ссылки | Рефералы | T-8.1, T-2.3 | S |

**Итого MVP: 27 задач.** Оценка: S = маленькая (1–2 дня), M = средняя (2–4 дня), L = большая (4–7 дней).

### Post-MVP задачи (Phase 2)

| # | Задача | Эпик | Зависимости | Оценка |
|---|--------|------|-------------|--------|
| T-3.2 | Автопроверка фото (NSFW, face, stock) | Фото | T-3.1 | L |
| T-4.2 | Waitlist закрытого города | Города | T-4.1, T-10.1 | M |
| T-8.2 | Покупка зорок за Telegram Stars | Экономика | T-8.1, T-10.1 | M |
| T-8.3 | Подписка «Безлимит» | Экономика | T-8.2 | M |
| T-9.3 | Предпочтения на свидания | Профиль | T-9.1 | S |
| T-11.1 | Вопрос дня | Общение | T-7.2, T-10.2 | M |
| T-12.1 | Идея свидания | Общение | T-7.2, T-9.3, T-13.1 | M |
| T-13.1 | AI-генерация сообщений | AI | T-7.2 | L |
| T-14.1 | Мини-игра «20 дилемм» | Общение | T-7.2 | M |
| T-15.1 | «Диалог заглох» — темы | Общение | T-7.2, T-13.1 | M |
| T-17.2 | Admin API модерации | Модерация | T-17.1 | M |
| T-18.1 | Верификация по селфи | Верификация | T-3.1, T-8.1 | L |
| T-19.1 | Доска идей | Community | T-8.1 | M |
| T-21.1 | Центр безопасности | Безопасность | T-7.2 | S |

**Итого Post-MVP: 14 задач.**

---

## Порядок разработки MVP

### Волна 1 — инфраструктура (параллельно)

```
T-0.1  Инициализация проекта
  ├── T-0.2  Доменные сущности + EF Core
  ├── T-0.3  Формат ответов, ошибки
  └── T-4.1  Поиск городов (seed данных)
```

### Волна 2 — авторизация и профиль

```
T-1.1  Telegram auth middleware
  ├── T-8.1  Кошелёк зорок
  ├── T-3.1  Загрузка фото
  ├── T-16.1 Настройки приватности
  └── T-10.1 Telegram Bot API сервис
```

### Волна 3 — онбординг (последовательно)

```
T-2.1  Черновик онбординга
T-2.2  Согласие пользователя
T-2.3  Завершение онбординга
```

### Волна 4 — core loop

```
T-5.1  Алгоритм ленты ← самая сложная задача MVP
T-5.2  Свайпы и мэтчинг
T-5.3  Отмена свайпа
T-5.4  Фильтры ленты
```

### Волна 5 — мэтчи и контакт

```
T-6.1  Списки лайков
T-7.1  Список мэтчей
T-7.2  Хаб мэтча
T-7.3  Открытие контакта
T-7.4  Архивация мэтчей (+ background job)
```

### Волна 6 — дополнительное

```
T-9.1  Профиль
T-9.2  Интересы
T-10.2 Уведомления
T-16.2 Блокировка, пауза, удаление
T-17.1 Жалобы + shadowban
T-20.1 Реферальные ссылки
```

---

## Инструкция для чатов разработки

Каждая задача передаётся в отдельный чат со следующим контекстом:

1. **Номер задачи** (напр. T-5.1).
2. **Раздел backend-spec** — релевантный раздел из `blizka-backend-spec.md`.
3. **Экраны** — номера S-xx из макетов, к которым привязана задача.
4. **Зависимости** — какие интерфейсы/сервисы уже реализованы (передать файлы или описать контракты).
5. **Результат** — что должно быть на выходе: endpoint-ы, сервисы, миграции, тесты.

Пример промпта для чата:

> Задача T-5.2 «Свайпы и мэтчинг». Реализуй три endpoint-а: `POST /api/feed/{userId}/like`, `POST /api/feed/{userId}/dislike`, `POST /api/feed/{userId}/superlike`. При взаимном лайке создай Match. Суперлайк списывает зорки через `ISparksService`. Используй контракты из Blizka.Api. Бизнес-правила: [скопировать из spec раздел 6.2]. Зависимости: `ISparksService` (T-8.1) уже реализован, вот интерфейс: [вставить].
