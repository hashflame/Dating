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

**Результат:** Переход `onboarding → active`, начисление стартовых зорок.

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

**Зависимости:** T-2.1, T-2.2, T-3.1, T-8.1.

---

## Эпик 3 · Фотографии

### T-3.1 · Загрузка и управление фото `[MVP]`

**Экраны:** S-06.

**Результат:** Upload, хранение, удаление, переупорядочивание фото.

**Что сделать:**
- `POST /api/users/me/photos` — multipart upload, сохранение в S3-совместимое хранилище.
- `DELETE /api/users/me/photos/{photoId}`.
- `PATCH /api/users/me/photos/reorder` — `{ order: [id1, id2, ...], mainPhotoId }`.
- При загрузке: удалить EXIF из файла на сервере (библиотека `MetadataExtractor` или `SixLabors.ImageSharp`).
- Ресайз: генерация thumbnail (150px) и medium (600px).
- Ограничения: max 6 фото, max 10MB на файл, форматы jpg/png/webp.
- `POST /api/users/me/photos/import-telegram` — скачать аватар по `user.photo_url` из Telegram.

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

**Результат:** Полнотекстовый поиск по населённым пунктам.

**Что сделать:**
- Seed таблицы `City` — все населённые пункты Беларуси + крупные города Польши, Литвы, Латвии, России, Украины (диаспора).
- `GET /api/cities/search?q=Мінск&locale=ru` — trigram search (`pg_trgm`), limit 10.
- `POST /api/geo/detect` — reverse geocoding по координатам (Nominatim OSM или аналог).
- Ответ включает `isOpen` для каждого города (MVP: все города открыты, механика waitlist — post-MVP).

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

**Результат:** Endpoint ленты с базовым алгоритмом подбора.

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
