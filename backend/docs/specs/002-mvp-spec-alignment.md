# Spec 002: Сверка spec.md с реализованными задачами (T-0.1–T-5.3) и правки

**Status:** Implemented
**Date:** 2026-08-24

## Problem

Аудит показал, что за 11 задач decomposition.md, отмеченных ✅ Реализовано (T-0.1, T-0.2, T-0.3,
T-1.1, T-2.2, T-2.3, T-3.1, T-4.1, T-5.1, T-5.2, T-5.3 — плюс T-5.4, которая фактически сделана,
но не помечена), реализация разошлась с `spec.md` в двух разных смыслах:

1. **Контрактные расхождения** — форма ответа/имена полей отличаются от примеров в `spec.md`,
   но сама реализация самодостаточна и, вероятно, уже используется фронтендом (см. коммит
   «Разбор фидбека фронтенда по стенду»). Переделывать код под спеку — ломать интеграцию.
2. **Функциональные пробелы** — вещи, которые `spec.md` требует (или подразумевает), а ни код,
   ни одна задача в `decomposition.md` (включая POST-MVP) их не покрывают.

Отдельно: часть расхождений, которые выглядели как пробелы, на самом деле уже корректно
запланированы в POST-MVP задачах (T-3.2, T-4.2, T-6.1, T-8.3, T-9.3, T-17.2) — они **не входят**
в эту спеку, дублировать их не нужно.

Решения по стратегии (согласованы с пользователем):
- Для чисто контрактных расхождений — **`spec.md` правится под уже реализованный код**.
- Для функциональных пробелов без цифр в `spec.md` — **предлагаются конкретные MVP-значения**,
  а не открытый вопрос.
- Один документ на все области (не дробить на отдельные спеки).

## Scope

### In

- Правки текста `spec.md` под фактический контракт (раздел «A. Правки в spec.md» ниже).
- Функциональные доработки кода, не покрытые ни одной задачей `decomposition.md` (раздел
  «B. Правки в коде» ниже), с приоритетами P0–P2.
- Новые/изменённые поля доменных сущностей и enum-значения, нужные для этих доработок.

### Out

- Всё, что уже корректно запланировано в POST-MVP: автопроверка фото (T-3.2), закрытые города/
  waitlist-поля `region/type/openThreshold/waitlistCount` кроме тех, что явно перечислены ниже
  (T-4.2), `LikesReveal`-транзакции (T-6.1), лимиты подписки «Безлимит» — безлимитные свайпы и
  5 суперлайков/неделю (T-8.3), `datePreferences` в скоринге и на карточке (T-9.3),
  полноценный admin-флоу бана — `POST /api/admin/users/{id}/ban` с ролью `admin` (T-17.2),
  переименование `SparkTransactionType.IdeaSubmission` → `IdeaSubmitted` (согласовать вместе
  с T-19.1, чтобы не переименовывать дважды).
- Косметичное имя `Photo.Position` vs `SortOrder` в домене — не выходит наружу в DTO, менять не
  нужно.
- Переработка алгоритма скоринга ленты (веса интересов/расстояния/верификации) — уже осознанное
  MVP-приближение, задокументированное в T-5.1, вне скоупа этой спеки.
- Полноценный unmatch-флоу (API + побочные эффекты) — см. B10 и Deferred Decisions.
- Повторный запрос геолокации после отказа на онбординге (например, из настроек профиля) — см.
  B1 и Deferred Decisions.
- Сигнал пользователю о причине невидимости в ленте (например, «нет фото») — см. B5 и Deferred
  Decisions.

## A. Правки в `spec.md` (документация → под код, без изменений в коде)

| # | Раздел spec.md | Было в spec.md | Правим на (по факту в коде) |
|---|---|---|---|
| A1 | `POST /api/auth/telegram` (§2.1) | `initData` в теле JSON-запроса | `initData` передаётся в заголовке `X-Telegram-InitData`; тело запроса пустое |
| A2 | Ответ `POST /api/auth/telegram` (§2.1) | `{ accessToken, userStatus, onboardingStep, locale }` | `{ token, expiresAt, userId, status, isNewUser, locale }` — поля `token`/`status` вместо `accessToken`/`userStatus`; `locale` добавляется кодом (B7); `onboardingStep` осознанно не возвращается, фронт получает его через `GET /api/onboarding/draft` |
| A3 | `GET /api/cities/search` | Ответ `{ results: [...] }` | Общий конверт `ApiResponse<T>` из T-0.3 — `{ data: [...] }`, как и у всех остальных списковых эндпоинтов |
| A4 | `POST /api/geo/detect`, тело запроса | `{ lat, lng }` | `{ lat, lon }` |
| A5 | `POST /api/geo/detect`, ответ | Плоский `{ cityId, name, country }` | Вложенный `{ city: { id, name, country, isOpen }, detectedAddress }` |
| A6 | `PATCH /api/consent`, тело запроса | `{ type, version, ageConfirmed }` | `{ type, version, ageConfirmed }` — поле остаётся, но появляется в коде впервые этой спекой (B4), это не переименование, а реальный юридический пробел |
| A7 | Значения `ConsentType` | snake_case (`terms_and_privacy`) | camelCase (`termsAndPrivacyPolicy`) — общий `JsonStringEnumConverter` проекта (T-0.3), исключений по одному enum не делаем |
| A8 | Карточка ленты, поле `badges` | Массив бейджей (`fire`/`writes_first`/…) | Заменено на `compatibilitySummary: { datingGoalMatch, sharedInterestsCount, bothVerified }` — осознанное MVP-упрощение из T-5.1, бейджи из `spec.md` относятся к хабу мэтча (T-7.2), не к карточке ленты |
| A9 | `POST /api/feed/undo`, ответ | `{ undone, undosRemaining, restoredUserId }` | `{ action, userId, undosRemaining, sparksBalance }` — добавлять `undone: true` в код не будем (избыточно с `action`), правим только пример в спеке |
| A10 | «Счётчик отмен сбрасывается ежедневно» (§undo) | Календарный сброс в полночь | Скользящее окно 24 часа от момента каждой отмены — намеренное решение T-5.3, эквивалентно по ощущению пользователя, проще в реализации |
| A11 | Карточка ленты, поле `district` | Присутствует в примере | Убрать из примера — нет источника данных (пользователь район не вводит нигде в онбординге), ни одна задача его не заводит (см. B11) |

## B. Правки в коде (функциональные пробелы)

Приоритеты: **P0** — потеря данных/юридический риск, чинить сразу; **P1** — влияет на
основной сценарий MVP; **P2** — можно отложить в отдельный тикет после этой спеки.

### B1 · [P0] Координаты пользователя из онбординга не сохраняются

Шаг 3 онбординга (`OnboardingStep3Data`) принимает `cityId`, но не координаты, хотя
`spec.md` (§3.1, шаг 3) ожидает `coordinates: {lat, lng}`, а `User.Coordinates`
(PostGIS `Point?`) — реальное поле, используемое в скоринге ленты (T-5.1) и радиусе фильтров
(T-5.4). Сейчас оно остаётся `null` для всех, кто прошёл онбординг.

**Правка:** добавить `Coordinates: {Lat, Lng}?` в `OnboardingStep3Data`, сохранять в
`CompleteOnboardingCommandHandler.ApplyProfileData` в `User.Coordinates`. Геолокация — по
желанию пользователя (Telegram WebApp API), при отказе — `null`, фолбэк на `City.Coordinates`
(уже работает).

**Решено:** запрос координат — только на шаге 3, один раз. Повторный запрос после отказа (из
настроек профиля и т.п.) — вне скоупа этой спеки (см. Deferred Decisions).

### B2 · [P0] Бан без причины и срока

`User` не хранит `BanReason`/`BannedUntil`, `UserBannedException` несёт только `userId`.
403-ответ по `spec.md` (§2.1) — `{ reason, expiresAt }`. Полноценный admin-эндпоинт для
простановки причины — задача T-17.2 (POST-MVP, не запланирована к скорой реализации).

**Правка:** добавить `User.BanReason: string?`, `User.BannedUntil: DateTimeOffset?`;
`UserBannedException.Details` — прокинуть оба поля; `403`-тело `{ reason, expiresAt }`.

**Решено:** до T-17.2 значения проставляются модератором вручную через прямую запись в БД —
никакого admin-эндпоинта в этой спеке не заводим (это был бы урезанный дубль T-17.2, выпущенный
раньше срока). Для уже забаненных вручную (без этих полей) — `null`/`null` (бессрочно, без
причины) как значение по умолчанию.

### B3 · [P1] Дневной лимит свайпов и `remainingToday`

`spec.md` закладывает `remainingToday` в ответе `GET /api/feed`, а T-8.3 снимает лимит только
для подписчиков «Безлимит» — значит, у бесплатных пользователей лимит должен существовать уже
в MVP.

**Правка:** MVP-значение — **50 свайпов за скользящее окно 24 часа** (тот же паттерн, что уже
принят в T-5.3 для undo). Считать по `Swipe.CreatedAt >= now - 24h` для текущего пользователя.
`FeedResponse.RemainingToday: int`.

**Решено (код ответа при превышении):** `429 Too Many Requests`, тело —
`ApiErrorResponse` с кодом `DAILY_SWIPE_LIMIT_EXCEEDED` и дополнительным полем
`resetAt: DateTimeOffset` — момент, когда состарится самый старый из 50 свайпов в текущем окне
(`oldest.CreatedAt + 24h`), чтобы фронт мог показать обратный отсчёт.

**Решено (точка расширения T-8.3):** если у пользователя активна подписка — лимит не
применяется; сам чек подписки — скоуп T-8.3, здесь только оставить место для него (например,
раннее `if (subscriptionChecker is null || !await subscriptionChecker.IsActiveAsync(...))`
вокруг проверки лимита), не реализовывать заранее.

### B4 · [P1] `UserConsent.AgeConfirmed`

`spec.md` (§2.2 / §3, шаг согласия) требует явное подтверждение совершеннолетия отдельно от
факта принятия условий. Сейчас поля нет ни в `RecordConsentRequest`, ни в `UserConsent`.

**Правка:** добавить `UserConsent.AgeConfirmed: bool`, `RecordConsentRequest.AgeConfirmed`,
валидация — `ageConfirmed == true` обязателен при `Type == TermsAndPrivacyPolicy`, иначе `400
VALIDATION_ERROR`.

### B5 · [P1] Верифицированные и с фото по умолчанию в ленте

`spec.md` (§6.1) формулирует это как базовое правило ленты, а не опциональный фильтр — сейчас
оба условия (`VerifiedOnly`, `RequirePhoto` в `UserFilter`) по умолчанию `false`
(`UserFilterDefaults`, T-5.4).

**Важный нюанс:** верификация селфи (`User.IsVerified = true`) — фича T-18.1, **тоже не
реализована** (POST-MVP). Включить `VerifiedOnly: true` по умолчанию прямо сейчас означало бы
пустую ленту для всех, потому что верифицироваться физически нечем.

**Правка:**
- `RequirePhoto` — дефолт **`true`** уже сейчас: фото — фича T-3.1, готова.
- `VerifiedOnly` — дефолт остаётся `false` **до T-18.1**; добавить комментарий в
  `UserFilterDefaults` и note в decomposition.md T-18.1, что после её реализации дефолт должен
  переключиться на `true`.

**Решено:** пользователи без фото при этом релизе молча перестают попадать в чужие ленты — без
специального сигнала (поля вроде `hiddenFromFeedReason`) и без уведомления. Инфраструктуры
уведомлений (T-10.2) ещё нет, заводить её ради этого сигнала — вне скоупа (см. Deferred
Decisions).

### B6 · [P2] `User.TelegramUsername` не сохраняется

Парсится в `TelegramInitDataValidator`, но никуда не пишется.

**Правка:** добавить `User.TelegramUsername: string?`, сохранять/обновлять в
`AuthenticateTelegramUserCommandHandler` при каждом логине (юзернейм в Telegram может
меняться).

### B7 · [P2] `locale` в ответе `POST /api/auth/telegram`

Уже есть в JWT claims (T-1.1), но не в теле ответа.

**Правка:** `AuthTelegramResponse.Locale: string`, из того же значения, что уже кладётся в
JWT-claim.

### B8 · [P2] `UserStatus.Onboarding`

`spec.md` (§2.2) описывает `new → onboarding → active`, в коде статус всё время остаётся `New`
до самого перехода в `Active`.

**Правка:** добавить `UserStatus.Onboarding`; переход `New → Onboarding` — при первом
`PATCH /api/onboarding/draft`; `Onboarding → Active` — как и сейчас, при завершении (T-2.3).

**Решено:** без backfill-скрипта — пользователи, уже застрявшие в `New` с недооформленным
драфтом на момент деплоя, останутся в `New`, пока сами не продолжат/завершат онбординг. Проект
в раннем MVP, число таких пользователей мало, статус — вспомогательное поле для будущего admin
API (T-17.2), не влияет на бизнес-логику доступа.

### B9 · [P2] `nextReward.hint` и `userStatus` в ответе завершения онбординга

`OnboardingCompleteResponse` не содержит поясняющий текст (`hint`) для следующей награды и
итоговый `userStatus`.

**Правка:** `NextRewardResponse.Hint: string`; `OnboardingCompleteResponse.UserStatus`.

**Решено (источник текста):** новый `NextRewardHintCatalog` (`Blizka.App`) по образцу
`ErrorMessageCatalog` — статичный набор локализованных строк (ru/be/en) на каждый порог
`ProfileCompleteness` (например, «Добавьте ещё фото, чтобы получить бонус» на пороге 60%),
согласуется с `RequestLocaleResolver`, как и остальные тексты проекта.

### B10 · [P2] `MatchStatus.Unmatched` — только модель данных

`spec.md` (§25, `Match.Status` enum) закладывает `Unmatched` как отдельное значение. Ни одна
задача в `decomposition.md` явный «unmatch» (разрыв мэтча пользователем) не заводит.

**Решено:** в этой спеке заводится **только `MatchStatus.Unmatched` как значение enum**, без
API и без побочных эффектов. Полноценный `POST /api/matches/{id}/unmatch` (с блокировкой
повторного мэтча, видимостью для второго участника и т.д.) — отдельная продуктовая фича, не
эта спека (см. Deferred Decisions — это больше не Open Question, решение принято).

### B11 · [P2] `region`/`type` для городов

`spec.md` показывает эти поля в поиске городов, но `City` их не собирает, и ни одна задача
(включая T-4.2, которая добавляет только `openThreshold`/`waitlistCount`) их не заводит.

**Правка:** добавить `City.Region: string?`, `City.Type: enum {City, Town}` — статические,
проставляются при сидировании справочника (T-4.1 уже сидирует 28 городов Беларуси + диаспору
PL/LT/LV/RU/UA).

**Решено:** `{City, Town}` — достаточная гранулярность, отдельное значение для столицы не
заводим. `population` — не заводим (используется спекой только для отображения/сортировки, не
для бизнес-логики) — см. Deferred Decisions. `district` — исключается из `spec.md` (см. A11),
не из кода — данных для него нет вообще ни на одном уровне (не только в `City`, но и негде
пользователю его указать).

### B12 · [P2] Поля карточки ленты: `datingGoal`, `lastActive`

Данные (`User.DatingGoal`, `User.LastActiveAt`) уже есть в домене, просто не прокинуты в
`FeedCardDto`.

**Правка:** добавить оба поля в `FeedCardDto` — чистое прокидывание существующих данных, без
новой бизнес-логики.

## Domain Model (сводка новых полей/enum-значений)

- `User.Coordinates` — уже существует, теперь **заполняется** из онбординга (B1).
- `User.BanReason: string?`, `User.BannedUntil: DateTimeOffset?` (B2).
- `User.TelegramUsername: string?` (B6).
- `UserStatus.Onboarding` — новое значение enum (B8).
- `UserConsent.AgeConfirmed: bool` (B4).
- `MatchStatus.Unmatched` — новое значение enum, без сопутствующего API (B10).
- `City.Region: string?`, `City.Type: enum {City, Town}` (B11).

Все — обычные EF Core миграции (`AddColumn`, nullable/со значением по умолчанию для
существующих строк — `BanReason`/`BannedUntil`/`TelegramUsername`/`Region` nullable,
`AgeConfirmed` — миграция с дефолтом `true` для уже существующих согласий, раз они уже приняты
до появления этого поля).

## API Contract (сводка по эндпоинтам)

| Эндпоинт | Изменение |
|---|---|
| `GET /api/feed` | + `remainingToday: int`; `FeedCardDto` + `datingGoal`, `lastActive` (B12); дефолт `RequirePhoto=true` (B5) |
| `POST /api/feed/swipe` | При превышении лимита — `429`, `ApiErrorResponse { code: "DAILY_SWIPE_LIMIT_EXCEEDED", resetAt }` (B3) |
| `POST /api/auth/telegram` | Ответ + `locale: string` (B7) |
| `PATCH /api/onboarding/draft`, шаг 3 | Тело + `coordinates: {lat, lng}?` (B1) |
| `POST /api/onboarding/complete` | Ответ + `nextReward.hint: string`, `userStatus: string` (B9) |
| `PATCH /api/consent` | Тело + `ageConfirmed: bool` (обязателен для `termsAndPrivacyPolicy`) (B4) |
| Любой `[Authorize]`-эндпоинт для забаненного | `403`-тело `{ reason: string?, expiresAt: DateTimeOffset? }` (B2) |
| `GET /api/cities/search` | Элементы результата + `region: string?`, `type: "city"\|"town"` (B11) |

## Authorization

Без изменений. Ни один пункт B1–B12 не вводит новых ролей/claims — все эндпоинты остаются
доступны только владельцу ресурса через уже существующую модель (собственный JWT,
`[Authorize]`). Простановка `BanReason`/`BannedUntil` (B2) до T-17.2 выполняется вручную прямой
записью в БД, минуя API — новой авторизационной поверхности это не создаёт.

## Edge Cases & Failure Modes

- **B1**: пользователь не даёт доступ к геолокации в Telegram WebApp → `coordinates: null` в
  запросе, `User.Coordinates` остаётся `null`, скоринг падает на фолбэк по городу (уже так
  работает). Повторный запрос позже — вне скоупа.
- **B3**: гонка двух параллельных свайпов около границы лимита — не критично (лимит не платёжный,
  максимум пропустит 1 лишний свайп); отдельной блокировки не заводим, по аналогии с тем, что
  уже принято для undo (T-5.3 тоже не блокирует гонки на уровне БД).
- **B5**: пользователи без фото, зарегистрированные до этой правки, молча перестают появляться в
  чужих лентах — без сигнала и уведомления (решено выше). Прецедент — T-5.4 тоже не делает
  бэкафилл для `ShowGender`/`AgeRange`.
- **B8**: пользователи, уже находящиеся в статусе `New` с непустым `OnboardingDraft` на момент
  деплоя — не переводятся в `Onboarding` задним числом (решено выше, без backfill).
- **B2**: пока причина/срок не проставлены вручную (до первого ручного вмешательства модератора
  после этой спеки) — `403`-тело возвращает `{ reason: null, expiresAt: null }`, не ошибку.

## Non-Functional Requirements

- **B3**: обязателен индекс `IX_Swipes_FromUserId_CreatedAt` (или составной с учётом уже
  существующих индексов на `Swipe`) — счётчик суточного лимита считается запросом
  `COUNT(*) WHERE FromUserId = @userId AND CreatedAt >= @since` на каждом `GET /api/feed` и
  каждом `POST /api/feed/swipe`; без индекса — seq scan по таблице `Swipe`, растущей на каждый
  свайп каждого пользователя.
- Остальное — без изменений: те же миграции, тот же стиль тестов (юнит на handler,
  интеграционные на контроллер), тот же Russian-comments-стандарт из `CLAUDE.md`.

## Integrations

None. Ни один пункт спеки не затрагивает внешние сервисы (Telegram Bot API, S3-хранилище,
AI-сервис) — все изменения внутри собственного API и БД.

## Acceptance Criteria

**AC-1 (B1, координаты сохранены).** Given пользователь на шаге 3 онбординга передал
`coordinates: {lat, lng}` и Telegram WebApp выдал геолокацию, When онбординг завершается
(`POST /api/onboarding/complete`), Then `User.Coordinates` установлены и видны в
`GET /api/users/me`, а следующий `GET /api/feed` использует их в скоринге вместо
`City.Coordinates`.

**AC-2 (B1, отказ от геолокации).** Given пользователь не передал `coordinates` на шаге 3,
When онбординг завершается, Then `User.Coordinates` остаётся `null`, а скоринг ленты падает на
`City.Coordinates` (регресс уже существующего поведения не допускается).

**AC-3 (B2, тело 403).** Given модератор вручную проставил `User.BanReason`/`BannedUntil` в БД,
When забаненный пользователь вызывает любой `[Authorize]`-эндпоинт, Then ответ — `403` с телом
`{ reason, expiresAt }`, совпадающим со значениями в БД.

**AC-4 (B2, бан без причины).** Given пользователь забанен (`Status = Banned`), но
`BanReason`/`BannedUntil` не проставлены, When он вызывает `[Authorize]`-эндпоинт, Then ответ —
`403` с телом `{ reason: null, expiresAt: null }`, не `500`.

**AC-5 (B3, превышение лимита).** Given пользователь сделал 50 свайпов за последние 24 часа,
When он отправляет 51-й `POST /api/feed/swipe`, Then ответ — `429` с
`ApiErrorResponse { code: "DAILY_SWIPE_LIMIT_EXCEEDED", resetAt }`, где `resetAt` равен моменту
устаревания самого старого из этих 50 свайпов.

**AC-6 (B3, remainingToday).** Given пользователь сделал N < 50 свайпов за последние 24 часа,
When он вызывает `GET /api/feed`, Then ответ содержит `remainingToday = 50 - N`.

**AC-7 (B4, отсутствие ageConfirmed).** Given `PATCH /api/consent` с `type: termsAndPrivacyPolicy`
без `ageConfirmed: true`, When запрос отправлен, Then ответ — `400 VALIDATION_ERROR`.

**AC-8 (B4, успешное согласие).** Given `PATCH /api/consent` с `ageConfirmed: true`, When
запрос отправлен, Then `UserConsent.AgeConfirmed == true` сохранено в БД.

**AC-9 (B5, фото по умолчанию).** Given пользователь без фото и стандартные (не изменённые)
фильтры ленты, When другой активный пользователь вызывает `GET /api/feed`, Then пользователь
без фото не попадает в список кандидатов.

**AC-10 (B5, верификация не требуется).** Given ни один пользователь в системе не верифицирован
(T-18.1 не реализована), When любой пользователь вызывает `GET /api/feed` со стандартными
фильтрами, Then неверифицированные кандидаты по-прежнему попадают в ленту (`VerifiedOnly`
остаётся `false` по умолчанию).

**AC-11 (B6, username сохраняется).** Given у пользователя в Telegram задан `username`, When он
проходит `POST /api/auth/telegram`, Then `User.TelegramUsername` сохранён/обновлён этим
значением.

**AC-12 (B7, locale в ответе).** Given успешная аутентификация, When возвращается ответ
`POST /api/auth/telegram`, Then поле `locale` в теле совпадает с claim `locale` в выданном JWT.

**AC-13 (B8, переход в Onboarding).** Given пользователь в статусе `New` без сохранённого
`OnboardingDraft`, When он впервые вызывает `PATCH /api/onboarding/draft` после деплоя этой
спеки, Then `User.Status` становится `Onboarding`.

**AC-14 (B9, hint по локали).** Given пользователь с `Locale = "be"` завершает онбординг с
`ProfileCompleteness` ниже следующего порога, When строится ответ, Then `nextReward.hint`
возвращает белорусскоязычную строку из `NextRewardHintCatalog` для этого порога.

**AC-15 (B9, userStatus в ответе).** Given завершение онбординга переводит пользователя в
`Active`, When строится `OnboardingCompleteResponse`, Then `userStatus == "active"`.

**AC-16 (B10, enum-значение существует).** Given доменную сборку `Blizka.App`, When
инспектируется `enum MatchStatus`, Then значение `Unmatched` присутствует и ни один
существующий code path его не устанавливает (регресс-тест на отсутствие использования не
требуется — фиксируется отсутствием нового кода, ставящего это значение).

**AC-17 (B11, регион и тип города).** Given сид-данные городов включают `Region`/`Type`, When
вызывается `GET /api/cities/search`, Then каждый результат содержит `region` и `type`
(`"city"` или `"town"`), совпадающие с сид-данными.

**AC-18 (B12, поля карточки).** Given кандидат в ленте имеет заполненные `DatingGoal` и
`LastActiveAt`, When вызывается `GET /api/feed`, Then соответствующий `FeedCardDto` содержит
оба значения.

**AC-19 (A-правки, документация).** Given этот документ утверждён, When `spec.md` проверяется
построчно по разделу A (A1–A11), Then текст `spec.md` отражает фактический контракт без
расхождений — проверяется вручную при мёрже правок в `spec.md`, не автотестом.

**AC-20 (регрессия).** Given весь набор существующих тестов (`Blizka.UnitTests` +
`Blizka.IntegrationTests`) до этой спеки, When доработки B1–B12 внедрены, Then все они остаются
зелёными без модификации ожидаемого поведения вне описанного здесь скоупа.

## Deferred Decisions

| Решение | Выбранный fallback | Триггер пересмотра |
|---|---|---|
| Полноценный unmatch (API, побочные эффекты) — B10 | Только `MatchStatus.Unmatched` в enum, без API | Продукт явно запрашивает фичу «разматчиться» |
| Повторный запрос геолокации после отказа — B1 | Запрашивается только один раз, на шаге 3 онбординга | Появляется задача редактирования профиля (T-9.1) с локацией в скоупе |
| Сигнал «нет фото» пользователю, скрытому из ленты — B5 | Без сигнала, без уведомления | Появляется инфраструктура уведомлений (T-10.2) |
| `population` для городов — B11 | Не заводится | Понадобится сортировка/фильтр по населению |
| Admin-эндпоинт для бана до T-17.2 — B2 | Ручная запись в БД | T-17.2 берётся в работу |

## Open Questions

Пусто — все пункты, ранее требовавшие решения (unmatch-скоуп, стопгэп для бана, backfill
`Onboarding`, код ошибки лимита свайпов, сигнал об отсутствии фото, локализация hint, повтор
геолокации, гранулярность `City.Type`, индекс для лимита свайпов, разделы Authorization/
Integrations), закрыты решениями выше.

## Implementation Notes

Реализовано B1–B12 и A1–A11 одним проходом:

- Одна миграция `AddSpec002Alignment` покрывает все новые поля/enum-значения из раздела Domain
  Model (`User.BanReason/BannedUntil/TelegramUsername`, `UserConsent.AgeConfirmed`,
  `City.Region/Type`, индекс `IX_Swipes_FromUserId_CreatedAt`). Применена и проверена на локальном
  Postgres.
- B3 (дневной лимит свайпов): точка расширения под T-8.3 реализована как `ISubscriptionChecker` —
  интерфейс без реализации/регистрации в DI, конструкторный параметр со значением по умолчанию
  `null` (штатное поведение `Microsoft.Extensions.DependencyInjection` для неразрешённых
  необязательных зависимостей).
- B9 (`nextReward.hint`): `NextRewardHintCatalog` живёт в `Blizka.App` и принимает локаль как
  обычную строку (`user.Locale`), а не `ApiLocale` (тип `Blizka.Api`) — иначе нарушалось бы
  направление зависимостей слоёв.
- B11 (`City.Region`/`Type`): все 40 строк сида (T-4.1) получили `Type = City` — критерия для
  `Town` decomposition.md/spec.md не дают, различие вводится заделом на будущее. `Region` —
  область для городов Беларуси (кроме Минска — `null`, отдельная административная единица) и
  страна для диаспоры.
- Регресс: весь существующий набор тестов обновлён под новые сигнатуры (`UserBannedException`,
  `RecordUserConsentCommand`, `GetFeedQueryHandler`, `PatchOnboardingDraftCommandHandler`,
  `CompleteOnboardingCommandHandler`) плюс добавлены новые тесты на B1–B9, B12. Итог:
  `Blizka.UnitTests` 146/146, `Blizka.IntegrationTests` 77/77.
- `spec.md` выверена построчно по A1–A11 (AC-19).
