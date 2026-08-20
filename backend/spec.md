# Блізка — Backend Technical Specification

> **Платформа:** .NET 10 · Web API · PostgreSQL  
> **Клиент:** Telegram Mini App (React + TypeScript)  
> **Источник:** спецификация интерфейса v1.0 — 30 экранов  
> **Дата:** 2026-08-18

---

## 1. Обзор системы

Блізка — dating mini-app внутри Telegram для Беларуси. Бэкенд обслуживает полный цикл знакомства: регистрацию за 90 секунд, ленту с объяснимым алгоритмом подбора, помощь в первом сообщении и путь до реальной встречи.

Ключевые ограничения архитектуры:

- Приложение **не хранит переписку** и не видит её — только готовит текст, который пользователь сам вставит в Telegram.
- Оплата **только через Telegram Stars** — требование площадки для цифровых товаров.
- Три языка: русский (по умолчанию), белорусский, английский.
- Четыре состояния у каждого экрана на фронте: загрузка (скелетон), пусто (призыв к действию), ошибка (с причиной и кнопкой повтора), успех — бэкенд должен возвращать достаточно данных для отображения каждого состояния.

---

## 2. Аутентификация и авторизация

**Источник:** S-01 (Загрузка)

### 2.1 Валидация Telegram initData

Каждый запрос от Mini App содержит `initData` — подписанную строку от Telegram. Бэкенд валидирует HMAC-SHA256 подпись на каждый запрос через middleware.

```
POST /api/auth/telegram
```

**Request body:**
```json
{
  "initData": "query_id=...&user=...&auth_date=...&hash=..."
}
```

**Response:**
```json
{
  "accessToken": "jwt...",
  "userStatus": "new | onboarding | active | paused | banned",
  "onboardingStep": 3,
  "locale": "ru"
}
```

**Бизнес-правила:**
- `initData` проверяется HMAC-SHA256 через bot token.
- `auth_date` не старше 5 минут.
- Если пользователь новый — создаётся запись со статусом `new`, имя и аватар подтягиваются из объекта `user` в `initData`.
- Если пользователь в статусе `banned` — возвращается `403` с телом `{ reason, expiresAt }`.
- JWT выдаётся с TTL 24 часа, содержит `userId`, `telegramId`, `locale`.

### 2.2 Статусы пользователя

| Статус | Описание | Переходы |
|--------|----------|----------|
| `new` | Только что открыл приложение | → `onboarding` |
| `onboarding` | Не завершил регистрацию | → `active` |
| `active` | Полноценный пользователь | → `paused`, `banned` |
| `paused` | Аккаунт на паузе (S-51) | → `active` |
| `shadowbanned` | Скрыт из ленты до ручной проверки | → `active`, `banned` |
| `banned` | Заблокирован модератором | → `active` (апелляция) |
| `deleted` | Аккаунт удалён (soft delete, 30 дней) | — |

---

## 3. Онбординг

**Источник:** S-02, S-03, S-04, S-05, S-06, S-07

Пять обязательных шагов. Черновик сохраняется на каждом шаге — закрыл приложение, вернулся с того же места.

### 3.1 Черновик онбординга

```
PATCH /api/onboarding/draft
```

**Request body (шаг 1 — S-03 «Кто я»):**
```json
{
  "step": 1,
  "data": {
    "name": "Артём",
    "birthDate": "1999-05-14",
    "gender": "male"
  }
}
```

**Request body (шаг 2 — S-04 «Кого ищу»):**
```json
{
  "step": 2,
  "data": {
    "showGender": "female",
    "ageRange": { "min": 22, "max": 32 },
    "datingGoals": ["serious", "no_rush"]
  }
}
```

**Request body (шаг 3 — S-05 «Город»):**
```json
{
  "step": 3,
  "data": {
    "cityId": "uuid",
    "coordinates": { "lat": 53.9, "lng": 27.56 }
  }
}
```

**Request body (шаг 4 — S-06 «Фото»):**
```json
{
  "step": 4,
  "data": {
    "photos": ["photo-uuid-1", "photo-uuid-2"],
    "mainPhotoId": "photo-uuid-1"
  }
}
```

**Бизнес-правила:**
- Шаг 1: дата рождения → возраст ≥ 18, иначе `422`.
- Шаг 2: `datingGoals` — минимум один выбран.
- Шаг 3: город может быть любым населённым пунктом в мире (диаспора).
- Шаг 4: минимум 1 фото, максимум 6.
- Каждый шаг идемпотентен — повторный PATCH того же шага перезаписывает данные.

### 3.2 Завершение онбординга

```
POST /api/onboarding/complete
```

**Response:**
```json
{
  "userStatus": "active",
  "sparksAwarded": 50,
  "profileCompleteness": 35,
  "nextReward": {
    "threshold": 60,
    "reward": 2,
    "hint": "Добавьте третий промпт и голосовое приветствие"
  }
}
```

**Бизнес-правила (S-07):**
- При завершении начисляется ✦50 зорок.
- Профиль считается заполненным на 35% после базового онбординга.
- Возвращается информация о следующем пороге награды.
- Статус меняется `onboarding → active`.

### 3.3 Согласие пользователя

```
POST /api/users/me/consent
```

**Request body:**
```json
{
  "type": "terms_and_privacy",
  "version": "1.0",
  "ageConfirmed": true
}
```

**Бизнес-правила (S-02):**
- Согласие фиксируется с временной меткой — требование закона РБ №99-З.
- Без согласия кнопка «Начать» неактивна на фронте, а бэкенд отклоняет `POST /api/onboarding/complete` с `422`.
- Хранятся: `userId`, `consentType`, `consentVersion`, `timestamp`, `ipAddress`, `telegramId`.

---

## 4. Поиск городов и геолокация

**Источник:** S-05 (Город), S-74 (Город ещё не открыт)

### 4.1 Поиск города

```
GET /api/cities/search?q=Минск&locale=ru
```

**Response:**
```json
{
  "results": [
    {
      "id": "uuid",
      "name": "Минск",
      "region": "Беларусь",
      "type": "city",
      "isOpen": true,
      "population": 2000000
    },
    {
      "id": "uuid",
      "name": "Минская область",
      "region": "аг. Прилуки, Беларусь",
      "type": "settlement",
      "isOpen": false,
      "waitlistCount": 42,
      "openThreshold": 300
    }
  ]
}
```

**Бизнес-правила:**
- Поиск по любому населённому пункту в мире — города, посёлки, агрогородки.
- Важно для диаспоры и небольших мест, которых нет в готовых справочниках.

### 4.2 Геолокация

```
POST /api/geo/detect
```

**Request body:**
```json
{
  "lat": 53.9006,
  "lng": 27.5590
}
```

**Response:**
```json
{
  "cityId": "uuid",
  "name": "Минск",
  "country": "Беларусь"
}
```

### 4.3 Статус города

```
GET /api/cities/{cityId}/status
```

**Response (город не открыт — S-74):**
```json
{
  "isOpen": false,
  "waitlistCount": 172,
  "openThreshold": 300,
  "progress": 0.57,
  "userOnWaitlist": true
}
```

**Бизнес-правила (S-74):**
- Город открывается при наборе порогового числа анкет (300).
- Счётчик публичный — пользователи видят прогресс.
- Пользователь в закрытом городе может: пригласить друзей, подписаться на уведомление об открытии, смотреть анкеты по всей Беларуси.
- При достижении порога — background job рассылает уведомления подписчикам.

---

## 5. Управление фотографиями

**Источник:** S-06 (Фото)

### 5.1 Загрузка фото

```
POST /api/users/me/photos
Content-Type: multipart/form-data
```

**Response:**
```json
{
  "id": "uuid",
  "url": "https://cdn.blizka.app/photos/...",
  "position": 1,
  "isMain": true,
  "checks": {
    "faceDetected": true,
    "nsfwScore": 0.02,
    "stockMatch": false,
    "status": "approved"
  }
}
```

**Бизнес-правила (S-06, notes):**
- Автопроверка при загрузке:
  - **NSFW-детектор** — score > 0.5 → отклонение с сообщением «Откровенный контент не допускается».
  - **Детектор лица** — нет лица → предупреждение «Не видно лица — загрузите другое фото» (не блокирует, но не может быть главным).
  - **Перцептивный хэш** против базы стоковых фото → отклонение.
- **EXIF удаляется на сервере** — геолокация, модель камеры, дата съёмки не сохраняются.
- Минимум 1 фото, максимум 6.
- Любое фото можно сделать главным (тап по звёздочке на фронте).
- Фото из Telegram: отдельный endpoint для импорта аватара из Telegram user object.

### 5.2 Импорт фото из Telegram

```
POST /api/users/me/photos/import-telegram
```

### 5.3 Переупорядочивание и главное фото

```
PATCH /api/users/me/photos/reorder
```

**Request body:**
```json
{
  "order": ["photo-uuid-3", "photo-uuid-1", "photo-uuid-2"],
  "mainPhotoId": "photo-uuid-3"
}
```

---

## 6. Лента (Feed)

**Источник:** S-10 (Дек), S-11 (Шторка анкеты), S-14 (Анкеты закончились), S-15 (Фильтры)

### 6.1 Получение ленты

```
GET /api/feed?limit=10
```

**Response:**
```json
{
  "cards": [
    {
      "userId": "uuid",
      "name": "Анна",
      "age": 24,
      "city": "Минск",
      "district": "Центр",
      "distanceKm": 2,
      "isVerified": true,
      "photos": [
        { "url": "...", "isMain": true },
        { "url": "..." },
        { "url": "..." }
      ],
      "compatibility": {
        "score": 84,
        "label": "strong_match",
        "matchedInterests": ["скалолазание", "настолки", "кофе"],
        "matchedValues": {
          "children": "match",
          "smoking": "match",
          "lifePace": "differ"
        },
        "summary": "Оба ищете серьёзные отношения, оба не курите"
      },
      "badges": ["🚭 не курит", "🍷 иногда пьёт", "🌅 жаворонок", "📏 168 см"],
      "interests": [
        { "name": "скалолазание", "emoji": "🧗", "isMatch": true },
        { "name": "настолки", "emoji": "🎲", "isMatch": true },
        { "name": "кофе", "emoji": "☕", "isMatch": true },
        { "name": "книги", "emoji": "📚", "isMatch": false },
        { "name": "подкасты", "emoji": "🎧", "isMatch": false }
      ],
      "datePreferences": [
        { "name": "активный отдых", "emoji": "🥾", "isMatch": true },
        { "name": "спокойные посиделки", "emoji": "☕", "isMatch": true },
        { "name": "квизы и настолки", "emoji": "🎲", "isMatch": true },
        { "name": "куда-то новое", "emoji": "🎭", "isMatch": false }
      ],
      "prompts": [
        {
          "question": "ЛУЧШЕЕ МЕСТО В МОЁМ ГОРОДЕ",
          "answer": "Крыша на Октябрьской в семь вечера, когда ещё светло, но уже не жарко"
        },
        {
          "question": "Я НЕРАЦИОНАЛЬНО ЛЮБЛЮ",
          "answer": "Возвращаться домой пешком через полгорода"
        }
      ],
      "datingGoal": "serious",
      "lastActive": "2026-08-18T10:30:00Z"
    }
  ],
  "remainingToday": 15,
  "exhausted": false
}
```

**Бизнес-правила:**
- Алгоритм подбора на сервере учитывает: цель знакомства (вес 0.15), интересы, ценности, расстояние, активность, предпочтения на свидания.
- Показываются только активные пользователи с проверенными фото.
- Расстояние вычисляется по координатам, но пользователь может скрыть его (показывается только город).
- Карточка в ленте показывает только базовое — имя, город, совместимость. Полная информация — в шторке (отдельный endpoint не нужен, данные отдаются в одном ответе).

### 6.2 Свайпы

```
POST /api/feed/{userId}/like
POST /api/feed/{userId}/dislike
POST /api/feed/{userId}/superlike
```

**Response (при мэтче — S-16):**
```json
{
  "action": "like",
  "isMatch": true,
  "match": {
    "matchId": "uuid",
    "userId": "uuid",
    "name": "Анна",
    "icebreakers": [
      { "type": "question_of_day", "label": "Вопрос дня", "effort": "10 секунд" },
      { "type": "minigame", "label": "Мини-игра", "effort": "2 минуты" },
      { "type": "date_idea", "label": "Идея", "effort": "1 тап" }
    ]
  },
  "sparksBalance": 49
}
```

**Бизнес-правила (S-16, notes):**
- При взаимном лайке создаётся match.
- На экране мэтча показываются 3 лёгких входа для начала общения с оценкой усилий.
- Суперлайк стоит зорки (определяется конфигурацией).

### 6.3 Отмена свайпа

```
POST /api/feed/undo
```

**Response:**
```json
{
  "undone": true,
  "undosRemaining": 2,
  "restoredUserId": "uuid"
}
```

**Бизнес-правила (S-10, notes):**
- Отмена последних 3 свайпов — бесплатно.
- Дальше решение окончательное, «как в жизни».
- Счётчик отмен сбрасывается ежедневно.
- Валидация на сервере — клиент не может отменить больше 3.

### 6.4 Фильтры

```
GET /api/feed/filters
PATCH /api/feed/filters
```

**Request body (PATCH):**
```json
{
  "ageRange": { "min": 22, "max": 32 },
  "maxDistanceKm": 25,
  "datingGoals": ["serious", "no_rush"],
  "requireFilledProfile": true,
  "activeWithinDays": 7,
  "requirePhoto": true,
  "advanced": {
    "verifiedOnly": true,
    "nonSmoker": false,
    "nonDrinker": false,
    "noChildren": false
  }
}
```

**Бизнес-правила (S-15):**
- Основные фильтры (тумблеры): заполненная карточка, активные за 7 дней, с фото.
- Дополнительные фильтры (чекбоксы в сворачиваемом блоке): только верифицированные, не курит, не пьёт, без детей.
- Фильтры применяются серверно при формировании ленты.
- Кнопка «Сбросить» восстанавливает дефолтные значения.

### 6.5 Анкеты закончились

Когда `exhausted: true` в ответе `GET /api/feed`, фронт показывает экран S-14 с тремя действиями:
1. **Расширить фильтры** — фронт открывает S-15.
2. **Пригласить друзей** — вызов `POST /api/referrals/invite`.
3. **Заполнить карточку до 100%** — фронт открывает профиль.

Бэкенд дополнительно отправляет Telegram-уведомление, когда появятся новые анкеты.

---

## 7. Симпатии (Likes)

**Источник:** S-21 (Симпатии)

### 7.1 Входящие лайки

```
GET /api/likes/incoming?revealed=false
```

**Response (заблюрено, до оплаты — S-21):**
```json
{
  "count": 7,
  "revealed": false,
  "unlockCost": 10,
  "preview": [
    { "blurredPhotoUrl": "..." },
    { "blurredPhotoUrl": "..." },
    { "blurredPhotoUrl": "..." },
    { "blurredPhotoUrl": "..." }
  ]
}
```

### 7.2 Разблокировка входящих лайков

```
POST /api/likes/incoming/reveal
```

**Response:**
```json
{
  "sparksSpent": 10,
  "sparksBalance": 37,
  "users": [
    { "userId": "uuid", "name": "...", "age": 25, "mainPhotoUrl": "..." }
  ]
}
```

**Бизнес-правила (S-21, notes):**
- Два таба: «Вам нравятся» (входящие) и «Вы нравитесь» (исходящие).
- Разблокировка входящих стоит ✦10 — открывает список **навсегда**, не за каждого отдельно.
- Раздел «Пропущенные» отсутствует — свайп окончательный.

---

## 8. Мэтчи и хаб мэтча

**Источник:** S-30 (Мэтчи), S-31 (Карточка мэтча — ХАБ)

### 8.1 Список мэтчей

```
GET /api/matches
```

**Response:**
```json
{
  "new": [
    {
      "matchId": "uuid",
      "user": { "userId": "uuid", "name": "Анна", "age": 24, "mainPhotoUrl": "..." },
      "matchedAt": "2026-08-18T08:30:00Z",
      "contactCost": 1,
      "writesFirst": false,
      "badge": "fire"
    },
    {
      "matchId": "uuid",
      "user": { "userId": "uuid", "name": "Катя", "age": 26, "mainPhotoUrl": "..." },
      "matchedAt": "2026-08-17T14:00:00Z",
      "contactCost": 0,
      "writesFirst": true,
      "badge": "writes_first"
    }
  ],
  "waitingForMessage": [
    {
      "matchId": "uuid",
      "user": { "userId": "uuid", "name": "Вера", "age": 23, "mainPhotoUrl": "..." },
      "contactOpenedAt": "2026-08-15T10:00:00Z",
      "badge": "contact_opened"
    }
  ],
  "archived": [
    {
      "matchId": "uuid",
      "user": { "userId": "uuid", "name": "Ника", "age": 28, "mainPhotoUrl": "..." },
      "archivedAt": "2026-03-04T00:00:00Z",
      "reason": "no_activity_7_days"
    }
  ]
}
```

**Бизнес-правила (S-30, notes):**
- Три секции: новые, ждут сообщения, архив.
- Мэтч уходит в архив через 7 дней без переписки — молча, без счётчика-угрозы.
- «Вернуть» из архива — бесплатно, всегда доступно.
- Тап по строке открывает хаб мэтча (S-31).
- «Пишет первой сама» — бейдж для пользователей с настройкой приватности «Запретить писать мне в Telegram» (S-51).

### 8.2 Хаб мэтча (детальная карточка)

```
GET /api/matches/{matchId}
```

**Response:**
```json
{
  "matchId": "uuid",
  "user": {
    "userId": "uuid",
    "name": "Анна",
    "age": 24,
    "city": "Минск",
    "lastActive": "2026-08-18T10:10:00Z",
    "telegramUsername": null,
    "mainPhotoUrl": "..."
  },
  "compatibility": {
    "score": 84,
    "details": "Вы обе любите скалолазание и настолки, обе не курите и ищете серьёзные отношения — из 12 параметров совпало 7"
  },
  "contactStatus": "locked",
  "contactCost": 1,
  "features": {
    "questionOfDay": {
      "available": true,
      "currentQuestion": "Смотрели новый фильм с Человеком-пауком? Как вам?",
      "myAnswer": null,
      "theirAnswer": null,
      "answeredBoth": false
    },
    "minigame": {
      "available": true,
      "played": false,
      "result": null
    },
    "dateIdea": {
      "available": true
    },
    "staleConversation": {
      "triggered": false,
      "daysSilent": 0
    }
  }
}
```

**Бизнес-правила (S-31, notes):**
- Хаб — центральный экран мэтча, из которого открываются все 5 веток: написать, вопрос дня, мини-игра, идея свидания, диалог заглох.
- `telegramUsername` возвращается только после оплаты (unlock).
- Вопрос дня виден в хабе прямо в превью — если оба ответили, ответы открыты.

---

## 9. Ветка «Написать»

**Источник:** S-32, S-33, S-34, S-35, S-36

### 9.1 Открытие контакта (оплата зорками)

```
POST /api/matches/{matchId}/unlock
```

**Response:**
```json
{
  "telegramUsername": "anna_k",
  "deepLink": "https://t.me/anna_k",
  "sparksSpent": 1,
  "sparksBalance": 46
}
```

**Бизнес-правила (S-32):**
- Стоимость: ✦1.
- После оплаты `telegramUsername` доступен навсегда для этого мэтча.
- Если у мэтча включена настройка «Запретить писать мне в Telegram» — контакт не открывается, пользователь может только ждать, пока мэтч напишет первым.

### 9.2 AI-генерация сообщения

```
POST /api/ai/generate-message
```

**Request body (S-34, S-35):**
```json
{
  "matchId": "uuid",
  "anchors": ["climbing", "rooftop_prompt"],
  "tone": "default",
  "attemptsUsed": 1
}
```

**Response:**
```json
{
  "variants": [
    {
      "id": "v1",
      "text": "Привет! Скалолазание в анкете — это зал или ты уже на настоящие скалы выбираешься?",
      "anchor": "climbing"
    },
    {
      "id": "v2",
      "text": "Крыша на Октябрьской в семь вечера — согласен полностью. У меня похожий пунктик про мосты.",
      "anchor": "rooftop_prompt"
    },
    {
      "id": "v3",
      "text": "Настолки и скалолазание в одной анкете — это редкое сочетание. За какую команду играешь на квизах?",
      "anchor": "combined"
    }
  ],
  "remainingAttempts": 4,
  "modifiers": ["shorter", "funnier", "no_question", "warmer"]
}
```

**Бизнес-правила (S-34, S-35, notes):**
- Пользователь выбирает до 2 «якорей» — конкретных деталей из анкеты собеседника (общий интерес, текст промпта).
- Сервер генерирует 3 варианта через LLM.
- Жёсткие prompt-правила: 2–4 предложения, опора на конкретную деталь анкеты, открытый вопрос в конце, **никаких комплиментов внешности**.
- Модификаторы (`shorter`, `funnier`, `no_question`, `warmer`) — повторный запрос с другим тоном.
- Лимит: 5 генераций на мэтч.
- Модель не выдумывает факты — варианты собраны из реальной анкеты собеседника.

### 9.3 Выбор и отправка сообщения

```
POST /api/matches/{matchId}/message-prepared
```

**Request body (S-36):**
```json
{
  "variantId": "v1",
  "text": "Привет! Скалолазание в анкете — это зал или ты уже на настоящие скалы?",
  "edited": true,
  "source": "ai_generated"
}
```

**Бизнес-правила (S-36, notes):**
- Бэкенд **не отправляет сообщение** — только фиксирует, что пользователь подготовил текст.
- Текст копируется в буфер на фронте, кнопка открывает Telegram deep link.
- После возврата в приложение: `POST /api/matches/{matchId}/message-sent-check` — «получилось написать?» (метрика, не вежливость).
- `source`: `self_written` | `ai_generated` | `question_of_day` | `minigame` | `date_idea` — для аналитики.

---

## 10. Вопрос дня

**Источник:** S-37 (Вопрос дня)

### 10.1 Получение вопроса

```
GET /api/matches/{matchId}/question-of-day
```

**Response:**
```json
{
  "questionId": "uuid",
  "text": "Смотрели новый фильм с Человеком-пауком? Как вам?",
  "publishedAt": "2026-08-18T19:00:00Z",
  "myAnswer": "Да, в выходные — экшн хорош, но концовку слили. Ты уже смотрела?",
  "theirAnswer": null,
  "bothAnswered": false,
  "archive": {
    "count": 12,
    "latestPreview": "Какой навык вы бы хотели получить мгновенно?"
  }
}
```

### 10.2 Ответ на вопрос

```
POST /api/matches/{matchId}/question-of-day/answer
```

**Request body:**
```json
{
  "questionId": "uuid",
  "text": "Ещё нет, но все спойлерят — придётся сходить на этой неделе"
}
```

**Response:**
```json
{
  "bothAnswered": true,
  "theirAnswer": "Да, в выходные — экшн хорош, но концовку слили."
}
```

**Бизнес-правила (S-37, notes):**
- Новый вопрос каждый день в 19:00 (background job).
- Вопросы на актуальную тему — новые фильмы, события в городе, свежие новости из мира интересов пары.
- Ответы открываются только когда ответили **оба**.
- Архив прошлых вопросов и ответов доступен.
- «Поделиться ответами в Telegram» — формирует готовый текст.

---

## 11. Мини-игра

**Источник:** S-38 (Мини-игра — результат)

### 11.1 Получение дилемм

```
GET /api/matches/{matchId}/minigame
```

**Response:**
```json
{
  "gameId": "uuid",
  "dilemmas": [
    { "id": 1, "optionA": "Утро", "optionB": "Вечер" },
    { "id": 2, "optionA": "Кошка", "optionB": "Собака" },
    { "id": 3, "optionA": "План", "optionB": "Экспромт" }
  ],
  "totalCount": 20
}
```

### 11.2 Отправка ответов

```
POST /api/matches/{matchId}/minigame/answers
```

**Request body:**
```json
{
  "gameId": "uuid",
  "answers": [
    { "dilemmaId": 1, "choice": "A" },
    { "dilemmaId": 2, "choice": "B" },
    { "dilemmaId": 3, "choice": "A" }
  ]
}
```

### 11.3 Результат

```
GET /api/matches/{matchId}/minigame/result
```

**Response:**
```json
{
  "matchedCount": 14,
  "totalCount": 20,
  "summary": "Вы обе выбираете горы вместо моря и книгу вместо сериала — а вот про утро и вечер мнения разошлись",
  "disagreements": [
    { "topic": "Утро или вечер", "myChoice": "Утро", "theirChoice": "Вечер" },
    { "topic": "Кошка или собака", "myChoice": "Собака", "theirChoice": "Кошка" },
    { "topic": "План или экспромт", "myChoice": "План", "theirChoice": "Экспромт" }
  ],
  "shareText": "Мы совпали на 14 из 20! Разошлись в утро/вечер, кошки/собаки и плане/экспромте 😄"
}
```

**Бизнес-правила (S-38, notes):**
- 20 быстрых пар «выбери одно из двух», по очереди.
- Результат показывает 3 темы разногласий — по ним легко написать шутку или вопрос.
- «Поделиться результатом в Telegram» — готовый текст в буфер.
- Доступна кнопка «Сыграть ещё раз» (новый набор дилемм).

---

## 12. Идея свидания

**Источник:** S-39 (Идея свидания)

```
GET /api/matches/{matchId}/date-ideas?city=Минск&maxBudget=30&currency=BYN
```

**Response:**
```json
{
  "sharedPreferences": [
    { "name": "активный отдых", "emoji": "🥾" },
    { "name": "спокойные посиделки", "emoji": "☕" },
    { "name": "квизы и настолки", "emoji": "🎲" }
  ],
  "ideas": [
    {
      "id": "uuid",
      "title": "Кофе и прогулка",
      "description": "Кофейня на Октябрьской, потом вдоль Свислочи до парка Горького",
      "estimatedCost": "~25 BYN",
      "estimatedDuration": "2 часа",
      "tags": ["активный отдых", "спокойные посиделки"],
      "inviteText": "Привет! Что скажешь — кофе на Октябрьской и прогулка вдоль Свислочи?"
    },
    {
      "id": "uuid",
      "title": "Квиз в баре",
      "description": "Вы обе отметили «квизы и настолки» — есть повод пойти командой из двух человек",
      "estimatedCost": "~20 BYN",
      "estimatedDuration": "3 часа",
      "tags": ["квизы и настолки"],
      "inviteText": "Привет! Пойдём командой из двух на квиз? Давно хотел попробовать"
    }
  ],
  "filters": {
    "city": "Минск",
    "maxBudget": 30,
    "currency": "BYN",
    "outdoorOnly": false,
    "homeAllowed": false
  }
}
```

**Бизнес-правила (S-39, notes):**
- Идеи строятся на основе совпадения по «Предпочтениям на свидания» из профилей обоих и общим интересам.
- Фильтры: город, бюджет, формат (на улице, можно дома).
- «Скопировать приглашение» — готовый текст для Telegram.
- «Другие идеи» — запрос новой порции.
- «Мы договорились о встрече» → `POST /api/matches/{matchId}/date-confirmed` — запускает follow-up опрос через 24 часа.

### 12.1 Подтверждение встречи

```
POST /api/matches/{matchId}/date-confirmed
```

**Бизнес-правила:**
- Через 24 часа — push-уведомление с опросом «Как прошла встреча?» (главный сигнал качества для алгоритма).
- Результат опроса влияет на вес алгоритма подбора.

---

## 13. Диалог заглох

**Источник:** S-41 (Диалог заглох — три темы)

```
GET /api/matches/{matchId}/stale-topics
```

**Response:**
```json
{
  "triggered": true,
  "daysSilent": 2,
  "topics": [
    {
      "text": "Ты говорила, что собираешься на скалодром на этой неделе — как всё прошло?",
      "source": "last_messages"
    },
    {
      "text": "Кстати, нашёл настолку, о которой ты говорила — «Каркассон» — играла?",
      "source": "shared_interests"
    },
    {
      "text": "Если пропадёшь ещё на пару дней — не обижусь, просто дай знать, что всё ок 🙂",
      "source": "gentle_check"
    }
  ]
}
```

**Бизнес-правила (S-41, notes):**
- Условие: 2 дня без ответа.
- Три темы генерируются на основе последних сообщений и общих интересов.
- Появляется **один раз** — не будем напоминать чаще.
- Каждая тема — готовый текст для копирования и отправки в Telegram.
- Паттерн «выбрать → скопировать → открыть Telegram» — единый для всех веток.

---

## 14. Профиль

**Источник:** S-40 (Мой профиль), S-43 (Интересы), S-42 (Предпочтения на свидания)

### 14.1 Получение профиля

```
GET /api/users/me
```

**Response:**
```json
{
  "userId": "uuid",
  "name": "Артём",
  "age": 27,
  "city": "Минск",
  "isVerified": true,
  "instagramLinked": true,
  "profileCompleteness": 65,
  "nextReward": {
    "threshold": 80,
    "reward": 2,
    "hint": "Добавьте третий промпт и голосовое приветствие"
  },
  "sparksBalance": 47,
  "photos": [],
  "interests": [],
  "datePreferences": [],
  "prompts": [],
  "datingGoal": "serious",
  "values": {},
  "badges": []
}
```

### 14.2 Редактирование профиля

```
PATCH /api/users/me/profile
```

Принимает частичное обновление полей: `name`, `birthDate`, `gender`, `bio`, `height`, `smoking`, `drinking`, `chronotype`, `prompts`, `voiceGreeting`.

### 14.3 Интересы

```
GET /api/interests/catalog
PATCH /api/users/me/interests
```

**Request body (PATCH — S-43):**
```json
{
  "interestIds": ["climbing", "board_games", "books", "podcasts", "coffee", "cooking", "hitchhiking", "new_countries", "running"]
}
```

**Бизнес-правила (S-43, notes):**
- Интересы сгруппированы по категориям: спорт, досуг, еда, путешествия, музыка, наука.
- Редкие интересы работают лучше: по «керамике» находятся свои, по «музыке» — все подряд.
- Пользователь может добавить свой интерес, если не нашёл в каталоге.
- Поиск по интересам.

### 14.4 Предпочтения на свидания

```
PATCH /api/users/me/date-preferences
```

**Request body (S-42):**
```json
{
  "preferences": ["active_outdoors", "calm_hangout", "quizzes_board_games", "something_new"]
}
```

**Бизнес-правила:**
- Заполняется один раз в профиле, используется алгоритмом «Идеи свидания» (S-39) для обоих.

### 14.5 Просмотр «как видят другие»

```
GET /api/users/me/preview
```

Возвращает профиль в формате, идентичном карточке в ленте (S-10, S-11), но для самого пользователя.

---

## 15. Зорки (Sparks) — экономика

**Источник:** S-46 (Кошелёк зорок), S-07 (Начисление), S-40 (Профиль)

### 15.1 Кошелёк

```
GET /api/sparks/wallet
```

**Response:**
```json
{
  "balance": 47,
  "earnOptions": [
    { "type": "profile_completion", "reward": 2, "progress": 0.65, "threshold": 0.80, "label": "Заполните профиль до 80%" },
    { "type": "verification", "reward": 3, "completed": false, "label": "Верифицируйтесь по селфи" },
    { "type": "referral", "reward": 2, "label": "Пригласите друга" },
    { "type": "idea_submitted", "reward": 1, "usedThisMonth": false, "label": "Предложите идею на доске" }
  ],
  "history": [
    { "type": "registration_bonus", "amount": 50, "date": "2026-08-01T12:00:00Z" },
    { "type": "contact_unlock", "amount": -1, "matchName": "Вера", "date": "2026-08-15T10:00:00Z" },
    { "type": "referral", "amount": 2, "referredName": "Макс", "date": "2026-08-16T18:00:00Z" }
  ]
}
```

### 15.2 Таблица начислений и списаний

| Событие | Изменение | Лимит |
|---------|-----------|-------|
| Регистрация | +50 ✦ | одноразово |
| Профиль достиг 60% | +2 ✦ | одноразово |
| Профиль достиг 80% | +2 ✦ | одноразово |
| Профиль достиг 100% | +2 ✦ | одноразово |
| Верификация селфи | +3 ✦ | одноразово |
| Реферал (друг завершил онбординг) | +2 ✦ | без лимита |
| Идея на доске | +1 ✦ | 1 раз в месяц |
| Идея внедрена | +10 ✦ | без лимита |
| Открытие контакта мэтча | −1 ✦ | — |
| Суперлайк | −N ✦ | конфигурируется |
| Разблокировка входящих лайков | −10 ✦ | одноразово |

### 15.3 Покупка зорок за Telegram Stars

```
POST /api/sparks/purchase
```

**Request body (S-75):**
```json
{
  "package": "50_sparks",
  "telegramPaymentId": "..."
}
```

**Пакеты (S-75):**

| Пакет | Зорки | Цена (Stars) |
|-------|-------|-------------|
| small | 20 ✦ | 99 ⭐ |
| medium | 50 ✦ | 229 ⭐ |
| large | 120 ✦ | 499 ⭐ |

**Бизнес-правила (S-75, notes):**
- Зорки не сгорают и не привязаны к месяцу.
- Оплата — только Telegram Stars (требование площадки для цифровых товаров).
- Покупка верифицируется через webhook от Telegram.

---

## 16. Подписка «Безлимит»

**Источник:** S-76 (Блізка+ — безлимит)

### 16.1 Получение статуса подписки

```
GET /api/subscriptions/me
```

**Response:**
```json
{
  "active": true,
  "plan": "unlimited",
  "priceStars": 399,
  "period": "monthly",
  "nextBillingDate": "2026-09-18T00:00:00Z",
  "features": {
    "unlimitedMessages": true,
    "unlimitedSwipes": true,
    "superlikesPerWeek": 5,
    "invisibleMode": true
  }
}
```

### 16.2 Оформление подписки

```
POST /api/subscriptions/unlimited/activate
```

### 16.3 Отмена подписки

```
POST /api/subscriptions/unlimited/cancel
```

**Бизнес-правила (S-76, notes):**
- Единственный план: 399 ⭐ / месяц, без многоуровневых планов.
- Фичи: безлимитные сообщения, безлимитные свайпы, 5 суперлайков в неделю, режим невидимки.
- Отмена в любой момент — доступ до конца оплаченного периода.
- Оплата через Telegram Stars — `invoice_link` через Bot API.

---

## 17. Реферальная система

**Источник:** S-47 (Пригласить друга), S-74 (Город не открыт)

```
POST /api/referrals/invite
```

**Response:**
```json
{
  "inviteLink": "https://t.me/blizka_bot?start=ref_ABC123",
  "shareText": "Присоединяйся к Блізка — знакомства, которые доходят до встречи",
  "stats": {
    "invited": 5,
    "registered": 3,
    "sparksEarned": 6
  }
}
```

**Бизнес-правила:**
- +✦2 за каждого друга, завершившего онбординг.
- В закрытом городе (S-74) реферал дополнительно ускоряет открытие — счётчик 172/300.
- `inviteLink` — deep link на Telegram бота с реферальным кодом.

---

## 18. Верификация

**Источник:** S-49 (Верификация)

### 18.1 Запуск верификации

```
POST /api/verification/selfie
Content-Type: multipart/form-data
```

### 18.2 Статус верификации

```
GET /api/verification/status
```

**Response:**
```json
{
  "status": "pending | verified | rejected",
  "submittedAt": "2026-08-18T14:00:00Z",
  "completedAt": null,
  "rejectionReason": null
}
```

**Бизнес-правила (S-49):**
- Верификация по селфи — сравнение с загруженными фото (face matching).
- При успехе: +✦3, бейдж «Проверен» в профиле.
- Фильтр «Только верифицированные» (S-15) использует этот статус.

---

## 19. Доска идей (Community)

**Источник:** S-60 (Доска идей)

### 19.1 Список идей

```
GET /api/ideas?sort=hot&page=1
```

**Response:**
```json
{
  "ideas": [
    {
      "id": "uuid",
      "text": "Голосовое приветствие в анкете — по голосу сразу понятно, свой человек или нет",
      "authorName": null,
      "isAnonymous": true,
      "votes": 142,
      "myVote": true,
      "status": "implemented",
      "implementedBadge": "co_author"
    },
    {
      "id": "uuid",
      "text": "Совместный плейлист для мэтча — сразу видно вкусы и есть о чём поговорить",
      "authorName": "Марина",
      "isAnonymous": false,
      "votes": 96,
      "myVote": false,
      "status": "under_review"
    }
  ]
}
```

### 19.2 Создание идеи

```
POST /api/ideas
```

**Request body:**
```json
{
  "text": "Добавить возможность шарить музыкальные вкусы",
  "anonymous": true
}
```

### 19.3 Голосование

```
POST /api/ideas/{ideaId}/vote
DELETE /api/ideas/{ideaId}/vote
```

**Бизнес-правила (S-60, notes):**
- +✦1 за идею, раз в месяц.
- +✦10 и бейдж «Соавтор», если идею внедрили.
- Статусы идеи: `new`, `under_review`, `planned`, `implemented`, `declined`.
- Доска идей — отдельная вкладка нижнего меню.

---

## 20. Приватность и безопасность

**Источник:** S-51 (Приватность), S-54 (Центр безопасности)

### 20.1 Настройки приватности

```
GET /api/privacy/settings
PATCH /api/privacy/settings
```

**Request body (PATCH — S-51):**
```json
{
  "blockIncomingMessages": true,
  "invisibleMode": false,
  "hideDistance": true,
  "hideAge": false,
  "showLastActive": true
}
```

**Бизнес-правила (S-51, notes):**
- `blockIncomingMessages: true` — ваш @username не увидит никто, первыми пишете только вы. У мэтчей будет надпись «Пишет первой сама».
- `invisibleMode` — видит ленту, но не показывается другим (фича Безлимит).
- `hideDistance` — виден только город, расстояние в км скрыто.
- `showLastActive` — «был(а) недавно» видно/скрыто.

### 20.2 Блокировка пользователей

```
POST /api/users/{userId}/block
DELETE /api/users/{userId}/block
GET /api/users/me/blocked
```

### 20.3 Управление аккаунтом

```
POST /api/users/me/pause
POST /api/users/me/resume
DELETE /api/users/me/account
GET /api/users/me/data-export
```

**Бизнес-правила (S-51):**
- **Пауза:** профиль скрыт из ленты, мэтчи и данные сохраняются.
- **Удаление:** soft delete, данные хранятся 30 дней на случай восстановления, затем полностью удаляются.
- **Экспорт данных:** формирует архив с персональными данными (GDPR-подобное требование).

### 20.4 Центр безопасности

```
POST /api/safety/share-date-plan
```

**Request body (S-54):**
```json
{
  "matchId": "uuid",
  "place": "Кофейня на Октябрьской",
  "dateTime": "2026-08-20T18:00:00Z"
}
```

**Response:**
```json
{
  "shareText": "Иду на свидание! 📍 Кофейня на Октябрьской, 20 августа в 18:00. Анкета: [ссылка]. Если не выйду на связь через 3 часа — позвони мне!"
}
```

**Бизнес-правила (S-54):**
- Формирует сообщение с местом, временем и ссылкой на анкету мэтча — для отправки другу в Telegram.

---

## 21. Жалобы и модерация

**Источник:** S-13 (Жалоба)

### 21.1 Подача жалобы

```
POST /api/users/{userId}/report
```

**Request body:**
```json
{
  "reason": "scam",
  "comment": "Просит перевести деньги на карту",
  "blockUser": true
}
```

**Типы жалоб (S-13):**

| Код | Название | Приоритет |
|-----|----------|-----------|
| `fake_photos` | Фейк или чужие фото | normal |
| `scam` | Мошенничество, просит деньги | high |
| `underage` | Несовершеннолетний | critical |
| `insults` | Оскорбления | normal |
| `explicit` | Откровенный контент | high |
| `spam` | Спам и реклама | normal |
| `unsafe_meeting` | Небезопасное поведение на встрече | critical |

**Бизнес-правила (S-13, notes):**
- **3 жалобы за 24 часа** на одного пользователя → **автоматический shadowban** до ручной проверки.
- Пункт `unsafe_meeting` → приоритетная очередь модерации.
- Пункт `underage` → немедленная блокировка + ручная проверка.
- `blockUser: true` — одновременно блокирует пользователя в списке заблокированных.
- SLA: «Проверим в течение 12 часов».

### 21.2 Admin API — модерация

```
GET /api/admin/reports?status=pending&priority=critical
POST /api/admin/reports/{reportId}/resolve
POST /api/admin/users/{userId}/shadowban
POST /api/admin/users/{userId}/ban
POST /api/admin/users/{userId}/unban
```

**Request body (resolve):**
```json
{
  "action": "warn | shadowban | ban | dismiss",
  "reason": "Подтверждено мошенничество",
  "banDurationDays": null
}
```

---

## 22. Уведомления

### 22.1 Telegram-уведомления (через Bot API)

| Событие | Сообщение | Условие |
|---------|-----------|---------|
| Взаимный лайк | «У вас новый мэтч! Откройте Блізка, чтобы начать общение» | Всегда |
| Новые анкеты после исчерпания | «Появились новые анкеты в вашем городе» | Когда `exhausted` был `true` |
| Город открыт | «Мы запустились в Гомеле! Теперь можно знакомиться» | Waitlist count ≥ threshold |
| Оба ответили на вопрос дня | «Анна тоже ответила на вопрос дня — посмотрите, что совпало» | Оба ответили |
| Мэтч скоро в архив | «Вы давно не общались с Анной — напишите, пока мэтч не ушёл в архив» | 5 дней без активности |
| Опрос после встречи | «Как прошла встреча с Анной?» | 24 часа после `date-confirmed` |
| Вопрос дня обновлён | «Новый вопрос дня — ответьте и узнайте, что думает Анна» | 19:00, если есть активный мэтч |

### 22.2 Unread counts

```
GET /api/notifications/unread
```

**Response:**
```json
{
  "likes": 12,
  "matches": 2,
  "ideas": 0,
  "total": 14
}
```

---

## 23. Webhook — Telegram Stars

```
POST /api/webhooks/telegram-stars
```

**Бизнес-правила:**
- Верификация подписи через `X-Telegram-Bot-Api-Secret-Token`.
- Обработка событий: `successful_payment`, `refunded_payment`.
- Идемпотентность по `telegram_payment_charge_id`.
- При успехе — зачисление зорок или активация подписки.

---

## 24. Background Jobs

| Job | Расписание | Описание |
|-----|-----------|----------|
| `ArchiveStaleMatches` | каждые 6 часов | Мэтчи без активности > 7 дней → архив |
| `GenerateQuestionOfDay` | ежедневно 18:50 | Генерация и публикация вопроса дня для каждой пары |
| `DetectStaleConversations` | каждые 4 часа | 2+ дня без ответа → генерация тем, флаг `staleConversation.triggered` |
| `CityOpenCheck` | каждые 30 мин | Проверка waitlist ≥ threshold → открытие города, рассылка уведомлений |
| `PostDateSurvey` | каждый час | 24+ часа после `date-confirmed` → push опрос |
| `CleanupDeletedAccounts` | ежедневно 03:00 | Удаление данных пользователей с soft delete > 30 дней |
| `PhotoModerationQueue` | каждые 5 мин | Обработка очереди фото-проверок (NSFW, face, stock hash) |
| `ShadowbanAutoCheck` | каждые 2 часа | Проверка 3+ жалоб за 24 ч → автоматический shadowban |
| `SubscriptionRenewal` | каждый час | Проверка истекших подписок, webhook от Telegram Stars |
| `NotifyNewProfiles` | каждые 3 часа | Уведомление пользователей с `exhausted` лентой о новых анкетах |

---

## 25. Модели данных (Domain Entities)

### Users
```
User {
  Id: Guid
  TelegramId: long
  TelegramUsername: string?
  Name: string
  BirthDate: DateOnly
  Gender: enum (Male, Female)
  CityId: Guid
  Coordinates: Point?
  Status: enum (New, Onboarding, Active, Paused, Shadowbanned, Banned, Deleted)
  ProfileCompleteness: int (0-100)
  SparksBalance: int
  Locale: string
  ConsentAt: DateTime?
  ConsentVersion: string?
  CreatedAt: DateTime
  LastActiveAt: DateTime
  DeletedAt: DateTime?
}
```

### Photos
```
Photo {
  Id: Guid
  UserId: Guid
  Url: string
  Position: int
  IsMain: bool
  FaceDetected: bool
  NsfwScore: decimal
  StockMatch: bool
  Status: enum (Pending, Approved, Rejected)
  CreatedAt: DateTime
}
```

### Interests / Date Preferences
```
Interest {
  Id: string (slug)
  Category: string
  NameRu: string
  NameBe: string
  NameEn: string
  Emoji: string
  IsCustom: bool
}

UserInterest {
  UserId: Guid
  InterestId: string
}

DatePreference {
  Id: string (slug)
  NameRu: string
  Emoji: string
}

UserDatePreference {
  UserId: Guid
  PreferenceId: string
}
```

### Swipes / Matches
```
Swipe {
  Id: Guid
  FromUserId: Guid
  ToUserId: Guid
  Type: enum (Like, Dislike, Superlike)
  CreatedAt: DateTime
  UndoneAt: DateTime?
}

Match {
  Id: Guid
  User1Id: Guid
  User2Id: Guid
  MatchedAt: DateTime
  Status: enum (Active, WaitingForMessage, Archived, Unmatched)
  ArchivedAt: DateTime?
  ContactUnlockedBy: Guid?
  ContactUnlockedAt: DateTime?
  DateConfirmedAt: DateTime?
  StaleNotifiedAt: DateTime?
}
```

### Sparks
```
SparkTransaction {
  Id: Guid
  UserId: Guid
  Amount: int
  Type: enum (RegistrationBonus, ProfileCompletion, Verification, Referral, IdeaSubmitted, IdeaImplemented, ContactUnlock, Superlike, LikesReveal, Purchase)
  ReferenceId: Guid? (matchId, referralId, ideaId, paymentId)
  CreatedAt: DateTime
}
```

### Messages & Games
```
QuestionOfDay {
  Id: Guid
  TextRu: string
  TextBe: string
  TextEn: string
  PublishedAt: DateTime
}

QuestionAnswer {
  QuestionId: Guid
  UserId: Guid
  MatchId: Guid
  Text: string
  AnsweredAt: DateTime
}

Minigame {
  Id: Guid
  MatchId: Guid
  Status: enum (InProgress, Completed)
  CreatedAt: DateTime
}

MinigameAnswer {
  GameId: Guid
  UserId: Guid
  DilemmaId: int
  Choice: enum (A, B)
}
```

### Reports
```
Report {
  Id: Guid
  ReporterUserId: Guid
  ReportedUserId: Guid
  Reason: enum (FakePhotos, Scam, Underage, Insults, Explicit, Spam, UnsafeMeeting)
  Comment: string?
  Priority: enum (Normal, High, Critical)
  Status: enum (Pending, Resolved)
  Resolution: enum? (Warn, Shadowban, Ban, Dismiss)
  ResolvedAt: DateTime?
  ResolvedByAdminId: Guid?
  CreatedAt: DateTime
}
```

### Ideas
```
Idea {
  Id: Guid
  AuthorUserId: Guid
  Text: string
  IsAnonymous: bool
  Status: enum (New, UnderReview, Planned, Implemented, Declined)
  VoteCount: int
  CreatedAt: DateTime
}

IdeaVote {
  IdeaId: Guid
  UserId: Guid
  CreatedAt: DateTime
}
```

### Cities
```
City {
  Id: Guid
  Name: string
  Region: string
  Country: string
  Type: enum (City, Town, Settlement)
  IsOpen: bool
  OpenThreshold: int
  WaitlistCount: int
  Coordinates: Point
}

CityWaitlist {
  CityId: Guid
  UserId: Guid
  NotifyOnOpen: bool
  CreatedAt: DateTime
}
```

### Payments
```
TelegramPayment {
  Id: Guid
  UserId: Guid
  TelegramPaymentChargeId: string
  Type: enum (SparksPurchase, UnlimitedSubscription)
  AmountStars: int
  Package: string?
  Status: enum (Pending, Completed, Refunded)
  CreatedAt: DateTime
}

Subscription {
  Id: Guid
  UserId: Guid
  Plan: string
  IsActive: bool
  StartedAt: DateTime
  ExpiresAt: DateTime
  CancelledAt: DateTime?
}
```

---

## 26. Сквозные требования

### 26.1 Локализация
- Три языка: `ru` (по умолчанию), `be` (белорусский), `en` (английский для диаспоры).
- Язык определяется из `initData.user.language_code`, может быть переопределён в настройках.
- Все текстовые ответы API (ошибки, подсказки, вопрос дня) — на языке пользователя.

### 26.2 Четыре состояния экрана
Каждый endpoint возвращает достаточно данных для отображения:
1. **Загрузка** — фронт показывает скелетон.
2. **Пусто** — пустой массив + `emptyState` с призывом к действию.
3. **Ошибка** — структурированная ошибка с причиной и действием.
4. **Успех** — полные данные.

### 26.3 Формат ошибок
```json
{
  "error": {
    "code": "INSUFFICIENT_SPARKS",
    "message": "Недостаточно зорок для этого действия",
    "details": {
      "required": 1,
      "balance": 0
    },
    "action": {
      "type": "navigate",
      "target": "sparks_wallet"
    }
  }
}
```

**Ошибка объясняет, что делать** (правило из интерфейсной спецификации): «Не видно лица — загрузите другое фото», а не «Ошибка загрузки».

### 26.4 Rate Limiting
- Auth: 5 req/min
- Feed: 30 req/min
- AI generation: 10 req/min
- Photo upload: 20 req/min
- Reports: 10 req/hour

### 26.5 Пагинация
```json
{
  "data": [],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 142,
    "hasMore": true
  }
}
```

---

## 27. Стек и инфраструктура

| Компонент | Технология |
|-----------|------------|
| Runtime | .NET 10 |
| API | ASP.NET Core Minimal API |
| ORM | Entity Framework Core 10 |
| БД | PostgreSQL 16 + PostGIS |
| Кэш | Redis |
| Очередь задач | Hangfire / .NET BackgroundService |
| Хранилище файлов | S3-совместимое (MinIO / Cloudflare R2) |
| AI / LLM | OpenAI API / Anthropic API через HttpClient |
| Фото-проверки | ML.NET / внешний сервис (NSFW, face detection) |
| Аутентификация | Custom middleware (Telegram initData HMAC) |
| Payments | Telegram Bot API (Stars) |
| Уведомления | Telegram Bot API (sendMessage) |
