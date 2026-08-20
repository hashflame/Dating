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

**Зависимости:** нет.

---

### T-0.2 · Доменные сущности и EF Core конфигурация `[MVP]`

**Результат:** Все entity classes + EF configurations + миграции, БД создаётся и seed-ится. ✅ Реализовано.

**Важно:** файла `backend-spec.md` (раздел 25) в репозитории нет — есть только этот `decomposition.md`. Формы сущностей ниже собраны по крупицам из упоминаний в других разделах (T-1.1, T-2.x, T-5.x, T-7.x, T-8.1, T-9.x, T-11.1, T-14.1, T-17.1, T-19.1, T-20.1 и т.д.), а не скопированы из авторитетного списка полей. Значения enum-ов без явного якоря в тексте (`DatingGoal`, `Smoking`, `Drinking`, `Chronotype`), категории интересов и точные координаты городов — решения по умолчанию; задачи, которым реально принадлежит фича (T-4.1 для полного гео-справочника, T-9.3 для предпочтений на свидания, T-14.1 для каталога дилемм), могут их уточнить без новой фундаментальной миграции.

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
  - Убрано лишнее двойное буферирование в `ImportTelegramPhotoCommandHandler` (копирование уже полностью буферизованного `download.Content` в новый `MemoryStream`) — интерфейс `ITelegramAvatarDownloader`/`TelegramAvatarDownload` теперь явно документирует, что `Content` уже seekable с доступной `Length`.
  - `StorageOptions` (Endpoint/Bucket/PublicBaseUrl) получил `ValidateOnStart()` — по аналогии с `Jwt:Secret` в `ApiServiceCollectionExtensions`, чтобы неполный конфиг падал при старте хоста, а не 500-кой на первой реальной загрузке фото.

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

**Результат:** Like/dislike, создание мэтча при взаимном лайке.

**Что сделать:**
- `POST /api/feed/{userId}/like` — создать `Swipe(type: Like)`. Проверить: есть ли встречный лайк → если да, создать `Match`.
- `POST /api/feed/{userId}/dislike` — создать `Swipe(type: Dislike)`.
- `POST /api/feed/{userId}/superlike` — списать зорки, создать `Swipe(type: Superlike)`, проверить мэтч.
- При мэтче (S-16): вернуть `isMatch: true` + данные мэтча + icebreakers (три входа).
- Уникальность: `(FromUserId, ToUserId)` — нельзя свайпнуть одного человека дважды.
- Транзакция: создание свайпа + проверка мэтча + (опционально списание зорок) — одна DB-транзакция.

**Зависимости:** T-5.1, T-8.1.

---

### T-5.3 · Отмена свайпа `[MVP]`

**Экраны:** S-10 (notes).

**Результат:** Undo последних 3 свайпов.

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

---

## Эпик 6 · Симпатии

### T-6.1 · Списки лайков `[MVP]`

**Экраны:** S-21.

**Результат:** Входящие и исходящие лайки.

**Что сделать:**
- `GET /api/likes/incoming` — кто лайкнул меня (без мэтча). MVP: возвращает `count` и `blurredPreviews` (заблюренные фото). Полный список — после unlock.
- `GET /api/likes/outgoing` — кого лайкнул я.
- `POST /api/likes/incoming/reveal` — списать ✦10, открыть список навсегда.
- Флаг `User.LikesRevealed` (bool) — после разблокировки всегда показывать.
- Разблокировка открывает список навсегда — не за каждого отдельно.

**Зависимости:** T-5.2, T-8.1.

---

## Эпик 7 · Мэтчи и хаб

### T-7.1 · Список мэтчей `[MVP]`

**Экраны:** S-30.

**Результат:** Три секции мэтчей: новые, ждут сообщения, архив.

**Что сделать:**
- `GET /api/matches`:
  - `new` — `Status = Active`, `ContactUnlockedAt IS NULL`.
  - `waitingForMessage` — `ContactUnlockedAt IS NOT NULL`, нет подтверждения отправки.
  - `archived` — `Status = Archived`.
- Бейджи: `fire` (высокий score), `writes_first` (настройка приватности партнёра), `contact_opened`.
- Сортировка: новые — по `matchedAt` DESC, ждут — по `contactUnlockedAt` DESC.

**Зависимости:** T-5.2.

---

### T-7.2 · Хаб мэтча `[MVP]`

**Экраны:** S-31.

**Результат:** Детальная карточка мэтча со статусами всех фич.

**Что сделать:**
- `GET /api/matches/{matchId}`:
  - Данные пользователя: имя, возраст, город, lastActive, mainPhoto.
  - `telegramUsername` — только если контакт разблокирован.
  - `compatibility` — score + текстовое описание совпадений.
  - `contactStatus`: `locked` | `unlocked` | `writes_first_only`.
  - `features`: статус каждой ветки (questionOfDay, minigame, dateIdea, staleConversation) — MVP: только `contactStatus`, остальные `available: false`.
- Проверка доступа: пользователь — участник мэтча.

**Зависимости:** T-7.1.

---

### T-7.3 · Открытие контакта (оплата зорками) `[MVP]`

**Экраны:** S-32, S-36.

**Результат:** Списание зорки, выдача Telegram username.

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

---

### T-7.4 · Архивация мэтчей `[MVP]`

**Экраны:** S-30 (notes).

**Результат:** Автоматическая и ручная архивация.

**Что сделать:**
- Background job `ArchiveStaleMatches` (каждые 6 часов):
  - Мэтчи с `Status = Active`, `ContactUnlockedAt IS NULL`, `MatchedAt` > 7 дней назад → `Status = Archived`.
  - Мэтчи с контактом, но без `message-sent-check` > 7 дней → `Status = Archived`.
- `POST /api/matches/{matchId}/archive` — ручная архивация.
- `DELETE /api/matches/{matchId}/archive` — вернуть из архива (бесплатно, всегда).

**Зависимости:** T-7.1.

---

## Эпик 8 · Экономика зорок

### T-8.1 · Кошелёк и транзакции `[MVP]`

**Экраны:** S-46, S-07.

**Результат:** Баланс, начисления, списания, история.

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

**Результат:** CRUD профиля, расчёт completeness.

**Что сделать:**
- `GET /api/users/me` — полные данные профиля + баланс зорок + completeness + nextReward.
- `PATCH /api/users/me/profile` — частичное обновление: name, bio, height, smoking, drinking, chronotype, prompts, datingGoal.
- При каждом обновлении: пересчёт `ProfileCompleteness`.
- Проверка порогов: если completeness впервые достигла 60%/80%/100% — начислить бонусные зорки.
- `GET /api/users/me/preview` — профиль в формате карточки ленты (как видят другие).
- Валидация: name 1–30 символов, prompts max 3 штуки × max 200 символов.

**Зависимости:** T-0.2, T-1.1, T-8.1.

---

### T-9.2 · Интересы `[MVP]`

**Экраны:** S-43.

**Результат:** Каталог интересов, выбор пользователем.

**Что сделать:**
- `GET /api/interests/catalog?locale=ru` — полный каталог по категориям.
- `PATCH /api/users/me/interests` — `{ interestIds: [...] }`.
- Пользовательские интересы: если `interestId` не найден в каталоге и `isCustom: true` — создать новый.
- Поиск по каталогу: `GET /api/interests/search?q=скал`.
- Пересчёт `ProfileCompleteness` после обновления.

**Зависимости:** T-9.1.

---

### T-9.3 · Предпочтения на свидания `[POST-MVP]`

**Экраны:** S-42.

**Результат:** Выбор предпочтений, использование в алгоритме.

**Что сделать:**
- Каталог предпочтений: `active_outdoors`, `calm_hangout`, `quizzes_board_games`, `something_new`.
- `PATCH /api/users/me/date-preferences` — `{ preferences: [...] }`.
- Учёт в алгоритме подбора (T-5.1) — совпадение предпочтений → бонус к score.
- Использование в «Идее свидания» (T-12.1).

**Зависимости:** T-9.1.

---

## Эпик 10 · Telegram-интеграция

### T-10.1 · Telegram Bot API сервис `[MVP]`

**Результат:** Инфраструктурный сервис для отправки сообщений и создания invoice.

**Что сделать:**
- `ITelegramBotService`:
  - `SendMessage(telegramId, text, parseMode)` — отправка уведомлений.
  - `CreateInvoiceLink(...)` — для покупки зорок и подписки.
  - `GetUserProfilePhotos(telegramId)` — для импорта аватара.
- HttpClient + retry policy (Polly).
- Rate limiting: Telegram допускает ~30 msg/sec в бота.
- Конфиг: `BotToken`, `WebhookSecret`, `PaymentProviderToken`.

**Зависимости:** T-0.1.

---

### T-10.2 · Уведомления `[MVP]`

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

**Результат:** Блокировка пользователей, пауза, удаление аккаунта.

**Что сделать:**
- Таблица `UserBlock` (blockerId, blockedId, createdAt).
- `POST /api/users/{userId}/block`, `DELETE /api/users/{userId}/block`.
- `GET /api/users/me/blocked` — список заблокированных.
- Блокировка влияет на ленту: заблокированный не появляется, не может лайкать.
- `POST /api/users/me/pause` — `Status = Paused`, скрыт из ленты, мэтчи сохраняются.
- `POST /api/users/me/resume` — `Status = Active`.
- `DELETE /api/users/me/account` — `Status = Deleted`, `DeletedAt = now()`. Soft delete 30 дней.
- `GET /api/users/me/data-export` — background job, формирует JSON-архив, отправляет ссылку в Telegram.

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
