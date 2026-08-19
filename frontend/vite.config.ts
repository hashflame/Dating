import { fileURLToPath, URL } from 'node:url'

import tailwindcss from '@tailwindcss/vite'
import basicSsl from '@vitejs/plugin-basic-ssl'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig(({ mode }) => ({
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
  },

  build: {
    target: 'es2022',
    sourcemap: true,
  },
}))
