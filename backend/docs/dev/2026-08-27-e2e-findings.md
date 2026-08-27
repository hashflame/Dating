# E2E-прогон бэкенда — находки (2026-08-27)

Контекст: ручной e2e-прогон поднятого локально бэкенда (Postgres+PostGIS, MinIO, `dotnet run`,
`DevLogin:Secret` + подписанный вручную Telegram initData) по цепочкам auth → онбординг → профиль →
лента/свайпы → мэтчи/зорки → приватность/блокировки/жалобы/рефералы. Три подтверждённых бага,
не пофикшены — только зафиксированы.

## 1. `POST /api/dev/reset-my-state` отдаёт 500 вместо 401 без валидного токена

- Файл: [`src/Blizka.Api/Controllers/DevController.cs:64`](../../src/Blizka.Api/Controllers/DevController.cs#L64)
- У `DevController` классовый `[AllowAnonymous]` (нужен остальным dev-методам с секретом), он
  перебивает метод-левый `[Authorize]` на `ResetMyState` — стандартное поведение ASP.NET Core
  (`[AllowAnonymous]` всегда побеждает, даже "издалека"). Это же подтверждает предупреждение
  компилятора `ASP0026` при сборке.
- Из-за этого `User.GetUserId()` (`src/Blizka.Api/Auth/ClaimsPrincipalExtensions.cs:8`) кидает
  `InvalidOperationException`, которое долетает до глобального обработчика как `500 INTERNAL_ERROR`.
- Воспроизведено:
  - без заголовка `Authorization` → 500 (`{"error":{"code":"INTERNAL_ERROR",...}}`)
  - с мусорным `Authorization: Bearer garbage.not.a.jwt` → 500
  - с валидным JWT → 204, работает как задумано
- Ожидаемое поведение (по докстрингу метода): `401`, как у остальных эндпоинтов с `[Authorize]`.
- Возможное решение: убрать классовый `[AllowAnonymous]` и расставить его точечно на
  `ReseedDemoData` вместо `ResetMyState`, либо явно проверять `User.Identity?.IsAuthenticated`
  в начале метода и возвращать `Unauthorized()`.

## 2. `age: 2025` в `GET /api/users/me` до завершения онбординга

- Файл: [`src/Blizka.App/UseCases/Users/UserProfileMapper.cs:58`](../../src/Blizka.App/UseCases/Users/UserProfileMapper.cs#L58)
  (метод `CalculateAge`, продублирован ещё в `GetUserProfileQueryHandler.cs:56` и
  `GetProfilePreviewQueryHandler.cs:44`)
- `CalculateAge(DateOnly birthDate)` не проверяет, что `birthDate` реально задан. Пока пользователь
  не прошёл шаг 1 онбординга, в БД лежит `BirthDate = DateOnly.MinValue` (`0001-01-01`), и функция
  считает `age = текущий_год - 1 = 2025` (для 2026 года).
- Воспроизведено: создал нового пользователя через настоящий (вручную подписанный) Telegram
  initData, до `PATCH /api/onboarding/draft` со step 1 вызвал `GET /api/users/me` →
  `"age": 2025, "birthDate": "0001-01-01"`.
- После завершения step 1 (или всего онбординга) значение считается верно.
- Риск: фронт может дёргать `users/me` в процессе онбординга (для прогресс-бара/статуса) и словить
  мусорное значение `age`.
- Возможное решение: возвращать `age: null` (сделать поле nullable в DTO), пока `BirthDate` не задан
  реально — либо явно проверять `birthDate == default` перед вычислением. Заодно вынести
  `CalculateAge` в одно общее место — сейчас 3 копии одной и той же логики.

## 3. `GET /api/users/{userId}` не учитывает блокировку — анкета заблокированного видна целиком

- Файл: [`src/Blizka.App/UseCases/Users/GetUserProfileQueryHandler.cs:20`](../../src/Blizka.App/UseCases/Users/GetUserProfileQueryHandler.cs#L20)
- Хендлер проверяет только `user is null || user.Status == UserStatus.Deleted`, но не смотрит в
  `IUserBlockRepository`. Для сравнения — `SwipeCommandHandler.cs:43` намеренно трактует взаимную
  блокировку как «пользователя не существует» (404 `SWIPE_TARGET_NOT_FOUND`), но эта же логика не
  применена к прямому просмотру анкеты.
- Воспроизведено: user1 заблокировал user5 (`POST /api/users/{id}/block` → 204). После этого:
  - `POST /api/feed/{id5}/like` → 404 `SWIPE_TARGET_NOT_FOUND` (правильно)
  - `GET /api/users/{id5}` → 200, полная анкета (фото, био, интересы) — доступна как ни в чём не бывало
- Риск: приватность — блокировка не защищает от просмотра профиля по прямой ссылке/id (например,
  подсмотренному раньше в ленте или из списка лайков).
- Возможное решение: добавить в `GetUserProfileQueryHandler` ту же проверку
  `userBlockRepository.ExistsEitherDirectionAsync(...)`, что уже есть в `SwipeCommandHandler`, и
  бросать `UserProfileNotFoundException` при блокировке в любом направлении.

## 4. `DELETE /api/users/me/photos/{photoId}` не защищает от удаления последнего фото

- Файл: [`src/Blizka.App/UseCases/Photos/DeletePhotoCommandHandler.cs`](../../src/Blizka.App/UseCases/Photos/DeletePhotoCommandHandler.cs)
- Онбординг требует минимум 1 фото для завершения регистрации
  (`CompleteOnboardingCommandHandler.EnsureStepsComplete`: `if (photoCount < 1) throw
  OnboardingIncompleteException("step4")`), но после этого ничто не мешает уже активному
  пользователю удалить все свои фото по одному через `DELETE
  /api/users/me/photos/{photoId}` — обработчик только переносит флаг `IsMain` на следующее фото,
  если удаляемое было главным, но не проверяет, что фото вообще остаётся хотя бы одно.
- Воспроизведено: у активного (прошедшего онбординг) пользователя было 2 фото — оба удалены двумя
  последовательными `DELETE`-запросами, оба вернули `204`. `GET /api/users/me/photos` → `[]`,
  `GET /api/users/me` → `"photos": [], "status": "active"`.
- Последствия: `GET /api/feed` фильтрует кандидатов без фото только если у смотрящего включён
  дефолтный фильтр `requirePhoto: true` (`FeedRepository.cs:79-81`, `query.Where(u =>
  u.Photos.Any())`) — это предпочтение смотрящего, а не жёсткое правило БД. Пользователь, у которого
  выключен `requirePhoto`, увидит в ленте карточку без единой фотографии.
- Возможное решение: в `DeletePhotoCommandHandler` бросать доменную ошибку (409/400), если это
  последнее фото пользователя — аналогично тому, как других полей-инвариантов профиля не дают
  занулить.

## 5. `POST /api/feed/{userId}/like` — 500 (нарушение unique constraint) после `undo` на мэтче с уже открытым контактом

Самый серьёзный из найденных: реальный, ничем не экзотический путь пользователя приводит к
краху запроса, а не просто к некорректному ответу.

- Файлы: [`src/Blizka.App/UseCases/Swipes/UndoSwipeCommandHandler.cs:43-50`](../../src/Blizka.App/UseCases/Swipes/UndoSwipeCommandHandler.cs#L43),
  [`src/Blizka.App/UseCases/Swipes/SwipeCommandHandler.cs:88-106`](../../src/Blizka.App/UseCases/Swipes/SwipeCommandHandler.cs#L88)
- Цепочка:
  1. У пользователей A и B уже есть активный мэтч с **открытым контактом**
     (`Match.ContactUnlockedAt` задан — например, из демо-сида, где часть мэтчей сразу создаются
     с открытым контактом).
  2. A вызывает `POST /api/feed/undo`. `UndoSwipeCommandHandler` находит последний активный свайп A
     (это может быть **тот самый свайп, который когда-то создал этот мэтч** — undo не привязан к
     конкретному свайпу, а просто берёт «последний активный»), помечает его `UndoneAt`, но матч
     **не удаляет**, потому что `match.ContactUnlockedAt is not null` (строка 46) — осознанно, чтобы
     не разрывать мэтч с уже открытым контактом. Однако Swipe-запись всё равно помечается отменённой.
  3. Теперь у A нет активного `Swipe(A→B)` (он `UndoneAt != null`), но `Match(A,B)` по-прежнему
     существует. Состояние рассинхронизировано: с точки зрения свайпов — не мэтчнуты, с точки зрения
     мэтчей — мэтчнуты.
  4. A (или что угодно, что вызовет повторный свайп — тот же like повторно доступен, потому что
     `swipeRepository.ExistsActiveAsync` в `SwipeCommandHandler:48` смотрит только на **активные**
     свайпы, а отменённый не активен) снова лайкает B: `POST /api/feed/{B}/like`.
  5. `SwipeCommandHandler` не кидает `ALREADY_SWIPED` (свайп же был отменён), доходит до
     `HasActiveMutualLikeAsync` (строка 89) — она true, потому что встречный свайп B→A никуда не
     делся. Хендлер пытается создать **новый** `Match(A,B)` (строка 95-103), не проверив, что такой
     мэтч уже есть → `INSERT` падает с `Npgsql.PostgresException 23505: duplicate key value violates
     unique constraint "IX_Matches_User1Id_User2Id"` → необработанное исключение → **500
     `INTERNAL_ERROR`** вместо любого осмысленного ответа.
- Воспроизведено дословно на демо-сиде: `TOKEN2` (user2) → `POST /api/feed/undo` (откатил старый
  swipe к user1, матч `e9e3bda6...` с открытым контактом не удалился) → `POST
  /api/feed/00000000-0000-0000-0a10-000000000001/like` → `500`. Стектрейс в логе хоста подтверждает
  `IX_Matches_User1Id_User2Id`.
- Возможные решения (любое из):
  - В `SwipeCommandHandler` перед созданием `Match` проверять `matchRepository.GetByUsersAsync(...)`
    и, если мэтч уже есть, не создавать новый (вернуть существующий как `matchResult` либо просто не
    выставлять `isMatch`/`match`, раз он не новый).
  - Либо в `UndoSwipeCommandHandler` не позволять отменить свайп, который лежит в основе мэтча с уже
    открытым контактом (запрещать `undo` в такой ситуации явной ошибкой, а не молча оставлять
    рассинхрон между `Swipe` и `Match`).

## Проверено и не баг (для справки)

- `date-ideas` без `?city=` отдаёт шаблонный плейсхолдер «вашем городе» без предлога «в» в двух
  шаблонах без привязки к предпочтению (`DateIdeaCatalog.cs:42-47`) — осознанный fallback
  (см. комментарий в файле), просто немного коряво грамматически на 2 из 10 шаблонов. Не критично.
- Проблемы с кириллицей в теле запроса при ручном тестировании curl'ом из git-bash — оказались
  артефактом кодировки консоли, не бэкенда (при передаче через файл с явным UTF-8 всё ок).
- `POST /api/geo/detect` с координатами Минска вернул «Англия, Соединённое Королевство» — оказалось,
  что DTO ожидает поле `lon`, а я передал `lng` (проигнорировано как неизвестное поле, `Lon` остался
  `0.0`, что и дало точку у побережья Англии). С правильным полем `lon` всё работает верно
  (`"detectedAddress":"Минск, Беларусь"`), включая валидацию диапазона (`-90..90`/`-180..180`) и
  `city: null` для точки вне ~50км от каталожных городов.
- Дубликат жалобы от одного репортера на одну и ту же анкету не защищён на уровне `CreateReportCommandHandler`
  (каждый вызов пишет новую строку в `Reports`), но авто-shadowban джоба (`ShadowbanAutoCheckJob`,
  порог 3+ жалобы за 24ч) считает **разных** репортеров через `.Select(new {ReportedUserId,
  ReporterUserId}).Distinct()` перед группировкой — так что один и тот же пользователь не может
  спамом жалоб добиться чужого shadowban'а в одиночку. Сам факт дублирующихся строк в `Reports` —
  не баг, просто задел для будущей модерации.

## Что покрыл прогон (без находок)

Auth (dev-login + настоящий Telegram initData с ручной HMAC-подписью, включая 410 `USER_DELETED`
при повторном входе удалённого аккаунта), полный онбординг (draft step 1-3 → photo → consent →
complete, включая проверки `ONBOARDING_INCOMPLETE` по каждому недостающему шагу), лента/свайпы
(like/dislike/superlike, дневной лимит, undo, повторный свайп → 409 `ALREADY_SWIPED`), мэтчи (hub,
unlock контакта идемпотентно, archive/unarchive, question-of-day, date-ideas, message-sent-check,
date-confirmed идемпотентно), зорки (баланс, списание на суперлайк/unlock/reveal, начисление за
онбординг, пагинация истории, валидация `page>=1`), каталоги (interests catalog/search,
date-preferences catalog, cities search — с корректной 400-валидацией пустого/отсутствующего `q`),
гео-детект (координаты → город + reverse geocoding, валидация диапазона lat/lon), фото (upload,
reorder, delete — **см. баг №4**), pause/resume (пауза корректно скрывает из чужой ленты через
`FeedRepository` фильтр `Status == Active`, но не блокирует собственный доступ пользователя к API —
похоже на осознанное поведение), data-export (202 Accepted), delete account (soft delete с 30-дневным
окном, задокументировано в коде), likes incoming/reveal (списание зорок, `revealed` флаг), блокировки
(block/unblock, блокировка корректно скрывает цель от свайпов — но не от прямого просмотра анкеты,
см. баг №3), жалобы (self-report запрещён, невалидная причина → 400, дубли не дают обойти
distinct-reporter подсчёт для shadowban), рефералы, уведомления, кросс-тенантный доступ к чужому
`matchId` (корректно 404, не палит существование).

## Не проверено (ограничения ручного HTTP-прогона)

- Сам `ShadowbanAutoCheckJob` (Quartz, раз в 2 часа) — логика подсчёта порога проверена по коду
  (см. выше), но реальное срабатывание джобы не наблюдалось вживую (требует либо ждать 2 часа, либо
  дергать Quartz-триггер напрямую, чего через HTTP API нет).
- Фоновые джобы `ArchiveStaleMatchesJob`, `GenerateQuestionOfDayJob` — не запускались/не
  наблюдались в рамках этого прогона.
- Загрузка фото через реальный Telegram photo import (`POST /api/users/me/photos/import-telegram`) —
  не тестировалась (нужен реальный доступный URL фото из Telegram).
