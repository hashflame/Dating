---
name: fe-telegram
description: Интеграция с Telegram Mini App во frontend: @tma.js/sdk-react только через shared/telegram, нативная кнопка «Назад», хаптика, тема, safe area, launch params и startParam, инициализация SDK, запуск и отладка внутри клиента. Используй при любой работе с нативным поведением Telegram. Triggers: Telegram, Mini App, SDK, back button, haptic, theme params, initData, startapp, webview.
---

# Telegram

SDK — `@tma.js/sdk-react`. Пакеты `@telegram-apps/*` устарели и не используются.

## Главное правило

Весь доступ к SDK — через `@/shared/telegram`. В `pages`, `domains`, `widgets`
импорт из `@tma.js/sdk-react` запрещён.

Нужна новая возможность Telegram — добавь обёртку в `src/shared/telegram/`
и экспортируй её из `index.ts`. Так вся работа с платформой остаётся в одном месте.

```tsx
import { useBackButton, useHaptic } from '@/shared/telegram' // ✅
import { backButton } from '@tma.js/sdk-react' // ❌ вне shared/telegram
window.Telegram.WebApp.showAlert('…') // ❌ никогда
```

## Инициализация

`src/shared/telegram/init.ts` вызывается один раз из `app/main.tsx` **до** рендера.
Порядок: `init()` → монтирование компонентов → `bindCssVars()` → `ready()`.

Не вызывай `init()` повторно и не монтируй компоненты SDK в React-компонентах.

## Кнопка «Назад»

Внутри Telegram нет браузерной кнопки назад — есть нативная кнопка клиента.

```tsx
const navigate = useNavigate()
const goBack = useCallback(() => void navigate({ to: ROUTES.home }), [navigate])

useBackButton(goBack)
```

- Показывается только там, где есть куда возвращаться. На корневом экране — `undefined`.
- Колбэк оборачивай в `useCallback`, иначе обработчик будет переподписываться.
- Хук сам скрывает кнопку при размонтировании.

## Хаптика

```tsx
const haptic = useHaptic()

haptic.tap() // нажатие
haptic.select() // выбор из списка
haptic.success() // успех: мэтч, сохранение
haptic.error() // ошибка
```

Ставь хаптику на значимые действия: свайп, лайк, мэтч, завершение шага онбординга.
Не ставь на каждый скролл и наведение. В клиентах без поддержки вызовы безопасны.

## Тема

Тему задаёт телефон: настройка системы → тема Telegram-клиента → переменные
`--tg-theme-*` на `:root` → токены приложения. Приложение своей темы не имеет
и переопределять клиентскую не должно.

Читать `themeParams` в компонентах не нужно — используй токены
(`bg-background`, `text-foreground`, …), скилл `fe-styles`.
Класс `.dark` на `<html>` и `color-scheme` для нативных элементов
синхронизируются автоматически в `shared/telegram/init.ts`.

Нужно знать тему в коде (другая картинка под тёмную) — хук `useIsDarkTheme`.

### Переключатель темы в браузере

В dev-сборке с `VITE_MOCK_TELEGRAM=1` в правом нижнем углу есть переключатель:
«как в системе» / светлая / тёмная. Устроен он так же, как настоящий клиент —
подменяет параметры темы и посылает SDK событие `theme_changed`, поэтому проверяет
ровно тот путь, который работает в Telegram.

Код: `shared/telegram/theme-mock.ts` (схемы и событие) и `app/dev/DevThemeToggle.tsx`
(кнопки). Внутри Telegram переключателя нет — там тему выбирает пользователь в телефоне.

### Dev-код не должен уезжать в релиз

Мок окружения и переключатель темы отрезаются на сборке проверкой
`import.meta.env.DEV`: Vite подставляет в неё `false`, ветка удаляется, а вместе
с ней и модули, на которые она ссылалась.

```tsx
if (import.meta.env.DEV && env.mockTelegram) {
  mockTelegramEnvironment()
}

{
  import.meta.env.DEV ? <DevThemeToggle /> : null
}
```

Два следствия, которые легко нарушить:

- Проверять надо именно `import.meta.env.DEV`, а не `env.isDev` — обычную
  переменную сборщик не сворачивает в константу и код останется в бандле.
- В таком модуле не должно быть вычислений на верхнем уровне
  (`const x = new URLSearchParams(...)`): сборщик считает модуль побочно-эффектным
  и не вырезает его. Заворачивай в функцию.

Проверить: `npm run build`, затем поискать в `dist/assets/*.js` строку из dev-кода.

## Safe area

Утилиты `pt-safe`, `pb-safe`, `min-h-viewport` — скилл `fe-styles`.
Липкая нижняя панель обязательно с `pb-safe`, иначе её перекроет системная зона.

## Launch params и startParam

- Данные пользователя — `getTelegramUser()` из `@/shared/telegram`.
- Deep link (`?startapp=…`) читается один раз при старте: добавь обёртку в
  `shared/telegram`, разбери значение и сделай навигацию. URL как источник
  состояния навигации не используем — роутер на memory history.

## Отладка внутри Telegram

```bash
npm run dev:https
cloudflared tunnel --url https://localhost:5173
```

`VITE_DEBUG_CONSOLE=1` включает мобильную консоль eruda (только dev).
`VITE_MOCK_TELEGRAM=1` позволяет открыть приложение в обычном браузере —
но initData фальшивый, реальное API его отвергнет.

## Особенности платформы, о которых легко забыть

- Вертикальный свайп закрывает мини-апп — он отключён в `init.ts`, не включай обратно.
- `pull-to-refresh` отключён в базовых стилях по той же причине.
- Приложение может быть открыто повторно из свёрнутого состояния — не рассчитывай
  на «холодный старт» при каждом входе.
- Версии клиентов различаются: перед использованием редкого метода проверяй
  `isAvailable()` и делай тихий fallback.
