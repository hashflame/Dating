import { fileURLToPath, URL } from 'node:url'

import tailwindcss from '@tailwindcss/vite'
import basicSsl from '@vitejs/plugin-basic-ssl'
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [react(), tailwindcss(), ...(mode === 'https' ? [basicSsl()] : [])],

    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },

    server: {
      host: true,
      port: 5173,
      // Туннели (cloudflared / ngrok) для запуска внутри Telegram.
      allowedHosts: ['.trycloudflare.com', '.ngrok-free.app', '.loca.lt'],
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
