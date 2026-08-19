# docs/specs

Спецификации фич, которых нет в исходном breakdown (`decomposition.md`, эпики T-0.x…T-21.x).

`decomposition.md` — авторитетный источник для MVP-объёма, но когда его эпики закончатся или потребуется фича вне исходного плана, спецификация для неё оформляется здесь как `docs/specs/<NNN>-<slug>.md` (использовать skill `spec`). Формат: описание проблемы, acceptance criteria, открытые вопросы — их нельзя оставлять до согласования.

- [`001-railway-deployment.md`](001-railway-deployment.md) — деплой `Blizka.Host` на Railway (Dockerfile, CI/CD, prod-конфигурация). Status: Implemented.
