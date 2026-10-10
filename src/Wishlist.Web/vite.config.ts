import path from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import basicSsl from '@vitejs/plugin-basic-ssl'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), basicSsl()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  // The basicSsl plugin serves the dev server over HTTPS so the API's
  // Secure-flagged refresh-token cookie is accepted and sent back in dev.
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:7277',
        changeOrigin: true,
        // Accept the ASP.NET Core local dev cert (self-signed, not trusted by Node's TLS stack).
        secure: false,
      },
    },
  },
})
