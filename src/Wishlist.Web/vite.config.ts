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
  server: {
    // Serve the SPA over HTTPS so the API's Secure-flagged refresh-token
    // cookie is accepted and sent back by the browser in dev.
    https: true,
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
