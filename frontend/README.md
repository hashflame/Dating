# Блізка — Frontend

Telegram Mini App на React + Vite.

## Запуск

```bash
npm install
cp .env.example .env
npm run dev
```

По умолчанию `VITE_MOCK_TELEGRAM=1` — приложение открывается в обычном браузере с
подменённым окружением Telegram. initData при этом фальшивый, реальное API его не примет.

Тема наследуется от системной настройки браузера. В правом нижнем углу — переключатель
«как в системе / светлая / тёмная» для проверки цветов; выбор сохраняется между
перезагрузками. В production-сборке ни мока, ни переключателя нет.

## Запуск внутри Telegram

Telegram открывает мини-апп только по https.

```bash
npm run dev:https                      # vite с самоподписанным сертификатом
cloudflared tunnel --url https://localhost:5173
```

Полученный https-адрес указать в BotFather → Bot Settings → Menu Button / Mini App.
Домены туннелей уже разрешены в `vite.config.ts` (`server.allowedHosts`).

Для отладки внутри Telegram-клиента: `VITE_DEBUG_CONSOLE=1` включает мобильную
консоль eruda (только в dev-сборке).

## Переменные окружения

Все переменные описаны в [`.env.example`](.env.example) и читаются
в единственном месте — [`src/shared/config/env.ts`](src/shared/config/env.ts).

## Команды

См. [`CLAUDE.md`](CLAUDE.md#команды) — там же архитектура и соглашения.

## Разработка через агента

Как формулировать задачи, чтобы получалось с первого раза —
[`docs/Obuchalka Sashki Vaibkodingu.md`](<docs/Obuchalka Sashki Vaibkodingu.md>).
