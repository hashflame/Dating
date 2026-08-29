# План: аналитика на фронтенде (PostHog)

Статус: план, код не написан. Разбит на истории — каждую можно отдать
`bmad-dev` отдельным запуском.

## Зачем

Понимать, кто пользуется приложением и что в нём делает: где отваливаются
в анкете, доходят ли до ленты, свайпают ли, пишут ли после мэтча — с разбивкой
по возрасту, полу, городу и платформе.

## Решение

**PostHog Cloud (free)** — 1M событий в месяц, воронки, retention, когорты,
дашборды. Хостить ничего не надо, бэкенд в этой задаче не участвует:
`posthog-js` шлёт события напрямую с клиента.

Регион — **EU** (`https://eu.i.posthog.com`): пользователи из Беларуси и ЕС,
данные не должны уезжать в США.

Демография берётся не из отдельной базы, а из **person properties**: один раз
при входе отправляем возраст/пол/город как свойства пользователя, дальше любой
график в PostHog фильтруется по ним.

Серверная часть (дублирование ключевых событий из `Blizka.App`) — отдельная
задача в `backend/`, вне этого плана.

## Ограничения, из которых растут решения

| Ограничение проекта                            | Что из этого следует                                                        |
| ---------------------------------------------- | --------------------------------------------------------------------------- |
| Мини-апп внутри Telegram-вебвью                | `persistence: 'localStorage'` — на cookies в вебвью полагаться нельзя       |
| Роутер с `memory history`, URL не меняется     | `capture_pageview: false`, экраны шлём сами через подписку на роутер        |
| `import.meta.env` только в `shared/config/env` | ключ PostHog добавляется в `envSchema`, больше нигде не читается            |
| Никаких сторонних SDK вне своей обёртки        | `posthog-js` импортируется **только** в `shared/analytics/` — как `@tma.js` |
| Код без потребителя запрещён                   | событие заводится вместе с местом вызова, «на будущее» — нет                |
| Дейтинг: на экране чужие фото и переписка      | session replay по умолчанию **выключен**                                    |
| Старт приложения и так не быстрый              | SDK грузится динамическим `import()` после первого рендера                  |

## Что появится в коде

```
src/shared/analytics/
  posthog.ts     единственное место импорта posthog-js: init, identify, capture, reset
  events.ts      типизированный словарь событий: имя → свойства
  track.ts       track(event) поверх posthog.ts; no-op, если аналитика выключена
  screen.ts      имя экрана по пути роута (для screen_viewed)
  index.ts       публичный экспорт: track, identifyViewer, resetAnalytics, типы

src/app/providers/AnalyticsProvider.tsx   инициализация, identify, подписка на роутер
```

`shared/config/env.ts` — два новых поля. `.env.example` — их описание.
`CLAUDE.md` — строка в «Что нельзя» и `shared/analytics/` в дереве структуры.

Почему `shared`, а не `domains/analytics`: аналитика не предметная область,
её зовут все слои, а `shared` — единственный слой, который импортируется откуда
угодно. Направление импортов не нарушается.

---

## История 1 · Каркас аналитики

**Цель:** событие, отправленное из любого места кода, долетает до PostHog;
без ключа в окружении приложение работает ровно как раньше.

Шаги:

1. `npm i posthog-js`.
2. `shared/config/env.ts`: `VITE_POSTHOG_KEY` (default `''`),
   `VITE_POSTHOG_HOST` (default `'https://eu.i.posthog.com'`).
   Поле `analytics: { key, host, enabled: key !== '' }`.
   Пустой ключ = аналитика выключена — так по умолчанию в dev и в любой сборке,
   где переменную не задали.
3. `.env.example`: обе переменные с комментарием, ключ пустой.
   Значение задаётся переменными окружения на Railway (prod и dev-стенд —
   **разные проекты в PostHog**, иначе dev-данные испортят метрики).
4. `shared/analytics/posthog.ts`:
   - `initAnalytics()` — `await import('posthog-js')`, конфиг:
     `api_host` из env, `autocapture: false` (клики по всему подряд съедят лимит
     и не дадут ничего осмысленного), `capture_pageview: false`,
     `capture_pageleave: false`, `persistence: 'localStorage'`,
     `person_profiles: 'identified_only'`, `disable_session_recording: true`.
   - `identify(id, props)`, `capture(name, props)`, `reset()`.
   - Модуль держит инстанс в переменной; пока его нет — вызовы молча игнорируются
     (аналитика никогда не роняет приложение и не заставляет ждать себя).
5. `shared/analytics/events.ts` — размеченный юнион имён и свойств
   (словарь ниже). Никаких свободных строк на вызове.
6. `shared/analytics/track.ts` — `track(event: AnalyticsEvent)`, единственная
   функция для всего кода.
7. `app/providers/AnalyticsProvider.tsx` — в `AppProviders`, внутри вызывает
   `initAnalytics()` в эффекте (после первого рендера, чтобы не задерживать
   показ первого экрана).

**Критерии приёмки**

- [ ] С пустым `VITE_POSTHOG_KEY` в сети нет ни одного запроса к posthog,
      чанк SDK не грузится.
- [ ] С заданным ключом событие видно в PostHog → Activity в течение минуты.
- [ ] `posthog-js` не импортируется нигде, кроме `shared/analytics/posthog.ts`.
- [ ] `npm run check` зелёный, размер основного чанка не вырос (SDK — отдельный чанк).

---

## История 2 · Кто пользуется: идентификация и свойства

**Цель:** в PostHog виден пользователь с возрастом, полом, городом
и платформой, и любую метрику можно разложить по этим свойствам.

Шаги:

1. `distinct_id` = `session.userId` из `domains/session`. Это внутренний id
   бэкенда, а не Telegram-id: он и так псевдонимный, наружу ничего лишнего
   не уезжает и хэшировать его не нужно.
2. `AnalyticsProvider` берёт `useSession()` и, когда сессия есть, зовёт
   `identify(session.userId, …)`.
3. Свойства пользователя — из `useViewer()` (`domains/viewer`), там уже есть
   всё нужное:
   - `age`, `gender`, `city` (`cityName`), `locale`, `status`
   - `photos_count`, `interests_count`, `profile_completeness`, `dating_goals`
   - `platform` и `tg_language` — из `@/shared/telegram`
   - `signup_is_new` — `session.isNewUser` (для когорты первой сессии)
     Отправлять при появлении и при изменении данных, не на каждый рендер.
4. Имя, фото, `telegramId`, `instagramHandle`, `bio`, тексты сообщений
   **не отправляются никогда**. В аналитике живут только перечисления и числа.
5. Переключение dev-пользователя в `DevPanel` вызывает `resetAnalytics()`,
   иначе события двух аккаунтов слипнутся в одного человека.

**Критерии приёмки**

- [ ] В PostHog → People у пользователя заполнены `age`, `gender`, `city`,
      `platform`; персональных данных нет.
- [ ] Любой график фильтруется по `gender` и по диапазону `age`.
- [ ] Смена пользователя в dev-панели создаёт нового человека, а не дописывает
      события в старого.

---

## История 3 · Экраны и онбординг

**Цель:** видно воронку анкеты по шагам — на каком поле люди уходят.

Шаги:

1. `screen_viewed` — подписка на `router.subscribe('onResolved')`
   в `AnalyticsProvider`. Свойство `screen` — стабильное имя из `ROUTES`
   (`feed`, `onboardingPhotos`, …), **не сырой путь**: в путях есть
   `$matchId`, а id мэтчей в аналитике не нужны.
2. `app_opened` — один раз на запуск, со свойством `start_param`
   (из Telegram launch params через `@/shared/telegram` — оно же покажет,
   откуда человек пришёл: инвайт, реклама, дип-линк).
3. Онбординг, в `pages/onboarding/ui/*`:
   - `onboarding_step_completed` — в `onSuccess` у `useSaveDraftStep`,
     свойства `step` (`about | preferences | city | photos | interests`)
     и `seconds_on_step`.
   - `onboarding_completed` — в `onSuccess` у `useCompleteOnboarding`,
     свойство `profile_completeness`.
4. Фото (`domains/photos`): `photo_uploaded` (`source: 'file' | 'telegram'`,
   `index`) и `photo_upload_failed` (`reason` — код ошибки из `ApiError`,
   не текст).
5. Согласие: `consent_accepted` на экране welcome.

Порядок вызова относительно согласия: до принятия согласия шлём только
`app_opened`, `screen_viewed` для welcome и `consent_accepted`;
`identify` со свойствами профиля — уже после.

**Критерии приёмки**

- [ ] В PostHog собирается воронка welcome → about → preferences → city →
      photos → interests → completed, и на каждом шаге видно отвал.
- [ ] Воронка раскладывается по `gender` и `platform`.
- [ ] В свойствах событий нет id мэтчей, пользователей и текстов.

---

## История 4 · Что используют: лента, лайки, мэтчи

**Цель:** видно, доходят ли до ленты, свайпают ли, кончаются ли карточки,
пишут ли после мэтча.

Шаги (события ставятся в существующие хуки доменов, а не в UI, — так они
не потеряются при переверстке экранов):

1. `domains/feed`:
   - `swipe` в `useSwipe.onSuccess`: `action` (`like | dislike`), `position`
     (номер карточки в текущей выдаче), `seconds_on_card`,
     `is_match` (из `SwipeResult`).
   - `feed_exhausted` — когда лента вернула пусто. Для дейтинга это ключевая
     метрика: кончились карточки — человек не вернётся.
   - `feed_filters_changed` в `useSaveFeedFilters`: `age_min`, `age_max`,
     `distance`.
   - `swipe_undone` в `useUndoSwipe`.
2. `domains/likes`: `likes_revealed` в `useRevealLikes` (`cost` в зорках).
3. `domains/matches`: `match_hub_opened`, `match_archived`.
4. `domains/messaging`: `chat_opened` из `useOpenChat`
   (`hours_since_match`, `is_first_message`), `message_blocked`
   (`reason` из `describe-block-reason`) — лимиты и есть узкое место продукта.
5. `domains/moderation`: `user_reported`, `user_blocked` (`reason`).

**Критерии приёмки**

- [ ] Считается конверсия `screen_viewed(feed)` → `swipe` → `match_created`
      → `first message`.
- [ ] Видно долю сессий, дошедших до `feed_exhausted`.
- [ ] Retention по `app_opened` строится за 7 и 30 дней.

---

## История 5 · Дашборд и проверка

**Цель:** метрики смотрят в PostHog, а не в сыром списке событий.

1. Дашборд «Продукт»: DAU/WAU/MAU, воронка онбординга, воронка ленты,
   retention 7/30, распределение по возрасту, полу, городу, платформе.
2. Дашборд «Здоровье»: `photo_upload_failed`, `message_blocked`,
   `feed_exhausted`, доля ошибок API.
3. Короткая справка в `docs/analytics.md`: где смотреть, что значит каждое
   событие, как добавить новое (правка `events.ts` + вызов, ничего больше).

---

## Словарь событий

| Событие                      | Где вызывается            | Свойства                                            |
| ---------------------------- | ------------------------- | --------------------------------------------------- |
| `app_opened`                 | `AnalyticsProvider`       | `start_param`, `platform`                           |
| `screen_viewed`              | подписка на роутер        | `screen`                                            |
| `consent_accepted`           | welcome                   | `consent_version`                                   |
| `onboarding_step_completed`  | `useSaveDraftStep`        | `step`, `seconds_on_step`                           |
| `onboarding_completed`       | `useCompleteOnboarding`   | `profile_completeness`                              |
| `photo_uploaded`             | `useUploadPhoto` / импорт | `source`, `index`                                   |
| `photo_upload_failed`        | там же                    | `reason`                                            |
| `swipe`                      | `useSwipe`                | `action`, `position`, `seconds_on_card`, `is_match` |
| `swipe_undone`               | `useUndoSwipe`            | —                                                   |
| `feed_exhausted`             | `useFeed`                 | `swipes_in_session`                                 |
| `feed_filters_changed`       | `useSaveFeedFilters`      | `age_min`, `age_max`, `distance`                    |
| `likes_revealed`             | `useRevealLikes`          | `cost`                                              |
| `match_hub_opened`           | `useMatchHub`             | `hours_since_match`                                 |
| `match_archived`             | `useArchiveMatch`         | `hours_since_match`                                 |
| `chat_opened`                | `useOpenChat`             | `hours_since_match`, `is_first_message`             |
| `message_blocked`            | `useMessageLimits`        | `reason`                                            |
| `user_reported` / `_blocked` | `domains/moderation`      | `reason`                                            |

DAU/MAU и retention PostHog считает сам из `app_opened` — отдельных событий
для них заводить не надо.

## Приватность

- Отправляются только перечисления, числа и внутренний `userId`.
  Ни имён, ни фото, ни текстов, ни `telegramId`.
- Session replay выключен: на экранах чужие анкеты и переписка. Если когда-то
  понадобится — включать точечно на онбординге и с маскированием, отдельной
  историей.
- Регион EU.
- До принятия согласия личность не идентифицируется.
- Упоминание аналитики добавить в текст политики (`pages/legal`) — проверить
  с юридическим текстом до релиза.

## Вне скоупа

- Серверные события из `Blizka.App` — отдельная задача в `backend/`
  (там же остаются достоверные `match_created` и платежи, которые с клиента
  теряются).
- Реверс-прокси для событий через свой домен.
- Feature flags и A/B — появятся сами, когда понадобятся: SDK уже подключён.
- Telegram Analytics (tganalytics.xyz) — ставится одной строкой и даёт
  статистику запусков внутри Telegram; отдельной историей, не конфликтует.

## Как проверять

- `npm run check` в каждой истории.
- Живая проверка: PostHog → Activity, пройти сценарий в `npm run dev:https`
  внутри Telegram и убедиться, что события приходят с ожидаемыми свойствами.
- Проверка на «выключено»: убрать ключ из `.env`, открыть вкладку сети —
  запросов к posthog быть не должно.
