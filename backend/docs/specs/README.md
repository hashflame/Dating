# docs/specs

Спецификации фич, которых нет в исходном breakdown (`decomposition.md`, эпики T-0.x…T-21.x).

`decomposition.md` — авторитетный источник для MVP-объёма, но когда его эпики закончатся или потребуется фича вне исходного плана, спецификация для неё оформляется здесь как `docs/specs/<NNN>-<slug>.md` (использовать skill `spec`). Формат: описание проблемы, acceptance criteria, открытые вопросы — их нельзя оставлять до согласования.

- [`001-railway-deployment.md`](001-railway-deployment.md) — деплой `Blizka.Host` на Railway (Dockerfile, CI/CD, prod-конфигурация). Status: Implemented.
- [`002-mvp-spec-alignment.md`](002-mvp-spec-alignment.md) — сверка `spec.md` с реализацией T-0.1–T-5.3: правки текста спеки под фактический контракт и функциональные пробелы (лимит свайпов, координаты из онбординга, причина/срок бана и т.д.). Status: Implemented.
- [`003-demo-seed-data.md`](003-demo-seed-data.md) — 10 фиксированных демо-пользователей на prod (`POST /api/dev/reseed-demo-data`) + dev-логин без Telegram (`X-Dev-Login-Secret`/`X-Dev-Login-TelegramId` на `POST /api/auth/telegram`), обе фичи выключены без `DevLogin:Secret`. Status: Approved.
