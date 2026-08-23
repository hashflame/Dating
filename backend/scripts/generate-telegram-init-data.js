#!/usr/bin/env node
// Генерирует валидно подписанный Telegram WebApp initData для локального/dev-тестирования
// POST /api/auth/telegram без реального Telegram-клиента. Использование — см. docs/dev/telegram-test-auth.md.
//
// Алгоритм подписи повторяет Blizka.App/Telegram/TelegramInitDataValidator.cs 1:1:
//   1. dataCheckString — поля (кроме hash) в виде "key=value", отсортированные по ключу, через "\n".
//   2. secretKey = HMAC-SHA256(key="WebAppData", message=botToken).
//   3. hash = HMAC-SHA256(key=secretKey, message=dataCheckString), hex.
//
// Никаких изменений в самом бэкенде это не требует и не ослабляет: initData проходит ту же
// валидацию (HMAC + свежесть auth_date), что и трафик от настоящего Telegram-клиента.

const crypto = require('crypto');

const botToken = process.env.BOT_TOKEN;
if (!botToken) {
  console.error('Задай переменную окружения BOT_TOKEN — значение Telegram:BotToken (см. Railway Variables или appsettings.Development.yaml). Токен не хранится и никуда не отправляется, кроме локального вычисления подписи.');
  process.exit(1);
}

const userId = process.argv[2] || '123456789';
const firstName = process.argv[3] || 'DevTester';

const fields = {
  auth_date: Math.floor(Date.now() / 1000).toString(),
  user: JSON.stringify({ id: Number(userId), first_name: firstName }),
};

const dataCheckString = Object.keys(fields)
  .sort()
  .map((key) => `${key}=${fields[key]}`)
  .join('\n');

const secretKey = crypto.createHmac('sha256', 'WebAppData').update(botToken).digest();
const hash = crypto.createHmac('sha256', secretKey).update(dataCheckString).digest('hex');

const initData = Object.entries({ ...fields, hash })
  .map(([key, value]) => `${key}=${encodeURIComponent(value)}`)
  .join('&');

console.log(initData);
