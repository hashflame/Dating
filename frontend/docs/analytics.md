# Аналитика

Продуктовые метрики живут в **PostHog Cloud, регион EU**
(`https://eu.i.posthog.com`). Организация — `Blizka`,
проект [«Default project»](https://eu.posthog.com/project/260838) (id `260838`),
ключ `phc_oATv5JSskmEjYk3CRtjq7ok9MBkVMM7Dxmsw9G8U4aex`.

Ключ публичный: он и так уезжает в браузерный бандл, им можно только слать
события. Секрет — личный API-токен, его здесь нет.

План внедрения и рассуждения, откуда взялись решения, — в
[`analytics-plan.md`](analytics-plan.md). Здесь только то, что нужно
в работе.

## Включение

Две переменные окружения (описаны в `.env.example`):

| Переменная          | Значение                                              |
| ------------------- | ----------------------------------------------------- |
| `VITE_POSTHOG_KEY`  | ключ проекта `phc_…`. **Пусто = аналитика выключена** |
| `VITE_POSTHOG_HOST` | `https://eu.i.posthog.com`, менять не нужно           |

Пустой ключ — состояние по умолчанию: SDK не скачивается, ни одного запроса
наружу не уходит.

**Что где задано:**

- Локально — в `.env` (он в `.gitignore`). Ключ уже прописан: сотри значение,
  если не хочешь слать события с локальной машины.
- **Прод и dev-стенд — переменная окружения на Railway. Это единственный шаг,
  который остаётся сделать руками: без него задеплоенное приложение не шлёт
  ничего.**

**Отдельного проекта под dev пока нет** — в организации один проект. Завести
второй можно только руками (Settings → Projects → New project), у MCP такого
инструмента нет. Пока его нет, локальные прогоны идут в тот же проект и
помечены как тестовый трафик: в настройках проекта стоит фильтр
`$host = localhost:5173`, а «Filter out internal and test users» включён по
умолчанию для новых графиков. У готовых графиков этот переключатель выключен
намеренно — иначе они были бы пустыми, пока настоящих пользователей нет.

## Как это устроено в коде

```
src/shared/analytics/
  posthog.ts             единственное место импорта posthog-js
  events.ts              словарь событий: имя → свойства
  track.ts               track(), identifyViewer(), resetAnalytics()
  screen.ts              имя экрана по пути роута
  use-elapsed-seconds.ts сколько секунд человек на экране/карточке

src/app/providers/AnalyticsProvider.tsx   init, identify, подписка на роутер
```

Правила, которые держат это в порядке:

- `posthog-js` импортируется **только** в `shared/analytics/posthog.ts` —
  проверяет `npm run lint` (`no-restricted-imports`), как и для `@tma.js`.
- SDK грузится динамическим `import()` после первого рендера и лежит
  отдельным чанком: старт приложения его не ждёт.
- События, случившиеся до конца загрузки SDK, копятся в очереди и уходят
  разом — иначе первые экраны (`splash`, `welcome`) терялись бы всегда.
- Продуктовые события стоят в **доменных хуках**, а не в UI: при переверстке
  экрана они не потеряются. В страницах живёт только то, что знает страница, —
  время на шаге и время на карточке.

## Как добавить событие

1. Добавить вариант в размеченный юнион `AnalyticsEvent`
   (`shared/analytics/events.ts`).
2. Позвать `track({ name: '…', … })` в том месте, которое его порождает.
3. Дописать строку в таблицу ниже.

Больше ничего: имя и свойства проверяет компилятор, свободных строк на вызове
нет. Событий «на будущее», без места вызова, не заводим.

## Словарь событий

| Событие                     | Где                            | Свойства                                                      |
| --------------------------- | ------------------------------ | ------------------------------------------------------------- |
| `app_opened`                | `AnalyticsProvider`            | `start_param`, `platform`                                     |
| `screen_viewed`             | подписка на роутер             | `screen` (ключ `ROUTES`)                                      |
| `consent_accepted`          | `WelcomePage`                  | `consent_version`                                             |
| `onboarding_step_completed` | страницы онбординга            | `step`, `seconds_on_step`                                     |
| `onboarding_completed`      | `InterestsPage`                | `profile_completeness`                                        |
| `photo_uploaded`            | `useUploadPhoto`, импорт из TG | `source`, `index`                                             |
| `photo_upload_failed`       | там же                         | `source`, `reason` (код `ApiError`)                           |
| `swipe`                     | `useSwipe`                     | `source`, `action`, `position`, `seconds_on_card`, `is_match` |
| `swipe_undone`              | `useUndoSwipe`                 | —                                                             |
| `feed_exhausted`            | `useFeed`                      | `swipes_in_session`                                           |
| `feed_filters_changed`      | `useSaveFeedFilters`           | `age_min`, `age_max`, `distance`                              |
| `likes_revealed`            | `useRevealLikes`               | `cost` (зорки; 0 при повторе)                                 |
| `match_hub_opened`          | `useMatchHub`                  | —                                                             |
| `match_archived`            | `useArchiveMatch`              | `archived` (`false` — возврат из архива)                      |
| `chat_opened`               | `useOpenChat`                  | `kind`, `sparks_spent`                                        |
| `message_blocked`           | `useOpenChat` (onError)        | `kind`, `reason`                                              |
| `user_reported`             | `useReportUser`                | `reason`, `also_blocked`                                      |
| `user_blocked`              | `useBlockUser`                 | —                                                             |

### Чего в событиях нет и почему

- `hours_since_match` у `match_hub_opened` и `chat_opened` — времени мэтча нет
  ни в `MatchHub`, ни в `ChatHandoff`. Появится в контракте (`backend/`) —
  добавим.
- `is_first_message` у `chat_opened` — приложение не доставляет сообщения и
  не знает истории переписки: она целиком в Telegram. Вместо этого шлём `kind`
  и `sparks_spent`.
- `position` у `swipe` — это номер свайпа **в сессии**, а не индекс в текущей
  выдаче: дека всегда показывает верхнюю карточку, и «индекс в выдаче» был бы
  всегда нулём.

## Приватность

- Уходят только перечисления, числа, флаги и внутренний `userId` бэкенда.
- Не уходят никогда: имена, фото, тексты сообщений, `telegramId`,
  `instagramHandle`, `bio`, id мэтчей и других пользователей.
- Session replay выключен: на экранах чужие анкеты и переписка.
- До согласия личность не идентифицируется: `identify` начинается только
  после того, как человек прошёл приветствие.
- Смена пользователя в панели разработки зовёт `resetAnalytics()` — события
  двух аккаунтов не слипаются.

## Что уже проверено вживую

Прогон `npm run dev` под dev-пользователем, 29.08.2026:

- `app_opened` (`platform: tdesktop`, `start_param: null`) — доходит;
- `$identify` с `age`, `gender`, `city`, `locale`, `status`, `photos_count`,
  `interests_count`, `profile_completeness`, `dating_goals`, `platform`,
  `tg_language`, `signup_is_new` — доходит, персональных данных в наборе нет;
- `screen_viewed` по всем пройденным экранам: `splash`, `feed`, `likes`,
  `matches`, `ideas`, `profile`, `matchHub` — по одному событию на переход,
  имена стабильные, **id мэтча в свойствах нет**;
- `match_hub_opened` — доходит.

Не проверены вживую действия, которые изменили бы реальные данные (свайп,
загрузка фото, отправка сообщения, жалоба): dev-стенд ходит в тот же API.
Код у них общий с проверенными — событие ставится в `onSuccess` мутации.

## Как проверять

1. `npm run check`.
2. Живая проверка: ключ в `.env`, `npm run dev:https`, пройти сценарий внутри
   Telegram, смотреть PostHog → Activity. События уходят пачками — до минуты
   задержки, это нормально.
3. Проверка «выключено»: убрать ключ, открыть вкладку сети — запросов
   к posthog быть не должно, чанк SDK не грузится.

## Вне скоупа фронта

Серверные события из `Blizka.App` (достоверные `match_created`, платежи —
с клиента они теряются) — отдельная задача в `backend/`.
