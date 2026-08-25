# Multi-stage build for the Blizka frontend — Telegram Mini App, статическая Vite-сборка,
# отдаётся nginx'ом (см. frontend/docs/deployment/railway.md).

FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Vite встраивает VITE_*-переменные в бандл на этапе сборки (см. src/shared/config/env.ts) —
# без ARG+ENV они были бы видны только процессу docker build, а не самому npm run build.
# Значения передаются как Docker build args (в Railway — переменные, помеченные
# "Available at build time", см. frontend/docs/deployment/railway.md).
ARG VITE_API_BASE_URL=""
ARG VITE_MOCK_TELEGRAM="0"
ARG VITE_DEBUG_CONSOLE="0"
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_MOCK_TELEGRAM=$VITE_MOCK_TELEGRAM
ENV VITE_DEBUG_CONSOLE=$VITE_DEBUG_CONSOLE

# ВРЕМЕННО (см. docs/real-backend.md, Вариант 3б) — только для задеплоенного dev-стенда фронта.
# На настоящем проде эти два build-arg'а не задаются на Railway, поэтому ARG остаётся на дефолте:
# NODE_ENV="production" — то же самое, что и сейчас без этой строки (`vite build` сам форсирует
# NODE_ENV=production, когда он не задан явно, поэтому пустая строка тут была бы не нейтральна
# и не менять поведение прода — только "production" безопасна как дефолт).
# VITE_DEV_LOGIN_SECRET="" — пустая строка выключает demo-вход, как и в src/shared/config/env.ts.
ARG NODE_ENV="production"
ARG VITE_DEV_LOGIN_SECRET=""
ENV NODE_ENV=$NODE_ENV
ENV VITE_DEV_LOGIN_SECRET=$VITE_DEV_LOGIN_SECRET

RUN npm run build

FROM nginx:1.27-alpine AS runtime

# NGINX_ENVSUBST_FILTER ограничивает envsubst только переменной $PORT — без фильтра образ
# по умолчанию подставляет все переменные окружения, что могло бы случайно задеть nginx-переменные
# вида $uri в конфиге (на практике не задело бы, т.к. $uri не существует как env var, но фильтр
# делает это гарантией, а не совпадением).
ENV NGINX_ENVSUBST_FILTER=^PORT$
COPY nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html

# Railway задаёт свой $PORT и ждёт, что контейнер слушает именно его — entrypoint образа nginx
# сам прогоняет envsubst по /etc/nginx/templates/*.template в /etc/nginx/conf.d/ при старте.
ENV PORT=8080
EXPOSE 8080
