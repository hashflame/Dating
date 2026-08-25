import { fileURLToPath, URL } from 'node:url'

import tailwindcss from '@tailwindcss/vite'
import basicSsl from '@vitejs/plugin-basic-ssl'
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

import { devTelegramAuth } from './vite/dev-telegram-auth'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [
      react(),
      tailwindcss(),
      devTelegramAuth({
        botToken: env.TELEGRAM_BOT_TOKEN,
        mockTelegram: env.VITE_MOCK_TELEGRAM === '1',
      }),
      ...(mode === 'https' ? [basicSsl()] : []),
    ],

    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },

    server: {
      host: true,
      port: 5173,
      // Туннели для запуска внутри Telegram. `.ngrok-free.dev` — новые адреса
      // ngrok, `.ngrok-free.app` — прежние.
      allowedHosts: ['.trycloudflare.com', '.ngrok-free.app', '.ngrok-free.dev', '.loca.lt'],
      // Через туннель страница приходит по https на 443, а HMR по умолчанию
      // стучится на 5173 и не доходит. Задаём публичный хост — и живая
      // перезагрузка работает так же, как на localhost.
      hmr: env.VITE_PUBLIC_HOST
        ? { protocol: 'wss', host: env.VITE_PUBLIC_HOST, clientPort: 443 }
        : undefined,
      // API проксируется на тот же origin: в dev нет CORS, а в коде — относительные пути.
      proxy: {
        '/api': {
          target: env.VITE_API_PROXY_TARGET || 'http://localhost:5000',
          changeOrigin: true,
        },
      },
    },

    build: {
      target: 'es2022',
      sourcemap: true,
    },
  }
})
