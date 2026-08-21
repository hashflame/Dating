# Деплой фронтенда Blizka на Railway

Тот же Railway-проект, что и API (см. `backend/docs/deployment/railway.md`), но отдельный сервис:
статическая Vite-сборка (Telegram Mini App) за nginx. Пайплайн полностью независим от бэкендового —
общий только Railway-проект и, при желании, `RAILWAY_TOKEN`.

## Почему отдельный сервис, а не то же самое, что API

Фронт и бэк — разные приложения с разным циклом деплоя (см. `CLAUDE.md` в корне: пакеты
самодостаточны). Frontend не проксирует `/api` в проде (в отличие от dev-сервера Vite, см.
`vite.config.ts`) — вместо этого собранный бандл обращается к API напрямую по его публичному
Railway-домену через `VITE_API_BASE_URL`, а CORS на бэкенде разрешает домен фронта явно
(`Cors__AllowedOrigins__N`, см. таблицу переменных в `backend/docs/deployment/railway.md`).

## Первоначальная настройка

1. **Сервис**: в том же Railway-проекте — New → Empty Service (как и для API, у Railway нет
   git-интеграции с GitLab). Билдер — **Dockerfile**, Dockerfile Path `Dockerfile`. Root Directory
   задавать не нужно — `frontend:deploy`-джоба в `.gitlab-ci.yml` делает `cd frontend` перед
   `railway up`, так что в Railway загружается уже папка `frontend/` как корень контекста сборки.
2. **Build-time переменные** (Vite встраивает `VITE_*` в бандл на этапе сборки, поэтому их нужно
   пометить в Railway как **Available at build time**, аналогично `SixLaborsLicenseKey` у API):
   - `VITE_API_BASE_URL` — публичный URL сервиса API, без завершающего `/`
     (например `https://api-production-3ead.up.railway.app`).
   - `VITE_MOCK_TELEGRAM=0` — прод обязан идти без мока Telegram-окружения.
   - `VITE_DEBUG_CONSOLE=0` — консоль eruda в проде не нужна.
3. **Healthcheck**: Path `/` (nginx отдаёт `index.html` на любой путь, см. `nginx.conf.template`).
4. **Публичный домен**: Settings → Networking → сгенерировать домен. Этот URL — то, что
   регистрируется в @BotFather как Web App URL Mini App'а.
5. **Добавить домен фронта в CORS бэкенда**: в переменных сервиса API задать следующий свободный
   `Cors__AllowedOrigins__N` (например `__1`, если `__0` уже занят) равным публичному домену фронта
   из п.4. Без этого браузер внутри Telegram будет ронять все запросы к API как cross-origin.
6. **Railway-токен для CI**: можно переиспользовать существующий `RAILWAY_TOKEN` бэкенда, если это
   Project Token (охватывает весь проект, оба сервиса) — тогда отдельный токен не нужен. Если у
   бэкенда Service Token (ограничен одним сервисом), создать новый: Railway → Project Settings →
   Tokens.
7. **Имя сервиса для CI**: добавить `RAILWAY_SERVICE_FRONTEND` в GitLab CI/CD Variables — имя
   фронтенд-сервиса в Railway (как оно называется в проекте), используется в
   `railway up --service "$RAILWAY_SERVICE_FRONTEND"`.
8. **`RAILWAY_PUBLIC_URL_FRONTEND`** (необязательно) — публичный URL из п.4, только для кнопки
   "View deployment" в GitLab UI.

## Подключение пайплайна в GitLab

У проекта сейчас в Settings → CI/CD → General pipelines → "CI/CD configuration file" стоит кастомный
путь `backend/.gitlab-ci.yml` — из-за этого GitLab видел только пайплайн бэкенда. Корневой
[`.gitlab-ci.yml`](../../../.gitlab-ci.yml) подключает оба пакетных файла через `include`, так что
путь в настройках нужно вручную вернуть на дефолтный `.gitlab-ci.yml` (или очистить поле) — иначе
фронтовые джобы (`frontend:build`, `frontend:deploy`) не запустятся ни разу.

## Пайплайн (`.gitlab-ci.yml`)

`build → deploy`, оба запускаются автоматически на `main` — в отличие от бэкенда, у `frontend:deploy`
нет `when: manual` (см. комментарий в `frontend/.gitlab-ci.yml`: фронт статический, состояния нет,
откатывать нечего, поэтому ручной гейт не нужен).

1. Пуш/мерж в `main` → `frontend:build` (`npm run check` + `npm run build`).
2. Следом сразу `frontend:deploy` — собирает Docker-образ и пушит его в Railway через `railway up`.

## Деплой без прав Maintainer в GitLab и без доступа к Railway-проекту бэкенда

Если в GitLab-репозитории ты Member/Developer (не Maintainer/Owner), добавить CI/CD Variables и
поменять "CI/CD configuration file" в Settings → CI/CD ты не сможешь — эти разделы настроек
GitLab открыты только Maintainer и выше. А раз в Railway ты раньше не работал, доступа в проект
бэкенда у тебя тоже, скорее всего, нет. Пайплайн выше рассчитан на то, что кто-то с более высокими
правами один раз всё это настроит — но задеплоить фронт можно и без него, вручную, со своей машины:

1. **Завести аккаунт Railway** (railway.app) — с ним ещё не работал никто из вашей команды,
   аккаунт свой, GitLab тут ни при чём: Railway не привязан к репозиторию, деплой всегда идёт
   через CLI-push локальных файлов (`railway up`), а не через git-интеграцию.
2. **Установить CLI и залогиниться**:
   ```bash
   npm install -g @railway/cli
   railway login
   ```
3. **Завести свой Railway-проект** (не обязательно тот же, что у бэкенда — если в тот тебя не
   пригласили, доступа туда всё равно нет):
   ```bash
   cd frontend
   railway init
   ```
   Билдер выставится автоматически по `Dockerfile` в папке. Либо, если тебя пригласят
   collaborator'ом в существующий Railway-проект бэкенда, вместо `init` — `railway link` и выбрать
   там сервис фронта (если его кто-то уже завёл).
4. **Задать build-time переменные** — обычные `railway variables set` создают только runtime-
   переменные; чтобы значение попало в сборку (Vite встраивает `VITE_*` в бандл на этапе `npm run
   build`, см. `Dockerfile` — там объявлены `ARG VITE_API_BASE_URL` и т.д.), их нужно передать как
   build args. Из UI Railway (Service → Variables) это делается галкой **"Available at build
   time"** рядом с переменной — из CLI такой галки нет, поэтому проще один раз зайти в Variables
   через веб-интерфейс после `railway init` и добавить там:
   - `VITE_API_BASE_URL` — публичный URL API-сервиса бэкенда (например
     `https://api-production-3ead.up.railway.app`, без `/` на конце) — им можно пользоваться, даже
     не имея доступа к тому Railway-проекту, если знаешь сам URL.
   - `VITE_MOCK_TELEGRAM=0`
   - `VITE_DEBUG_CONSOLE=0`
5. **Задеплоить**:
   ```bash
   railway up
   ```
   Дальше при каждом изменении фронта повторять `railway up` вручную с локальной машины — GitLab
   CI тут не участвует вообще, так что права Maintainer для этого не нужны.
6. **Сгенерировать публичный домен** (Settings → Networking) — понадобится и как Web App URL для
   @BotFather, и чтобы попросить владельца бэкенда добавить его в CORS.
7. **CORS на бэкенде — единственное, что нужно попросить у того, кто владеет этим Railway-
   проектом**: без добавления твоего домена в `Cors__AllowedOrigins__N` браузер внутри Telegram
   будет ронять все запросы к API как cross-origin, а этот параметр ты сам поменять не можешь, не
   имея доступа к сервису API.

Если позже получишь Maintainer-права в GitLab (или кто-то другой ими воспользуется) — можно
переключиться на автоматический пайплайн, описанный в разделах выше, тогда этот ручной способ
станет не нужен.

## Что не входит в эту итерацию

- Регистрация Web App URL в @BotFather — ручная задача отдельно.
- Staging-окружение.
- Автоматический (push-triggered) continuous deployment — деплой всегда manual, как у бэкенда.
