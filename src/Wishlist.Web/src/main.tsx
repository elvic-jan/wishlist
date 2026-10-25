// First: catch the browser's install offer even if it fires before React mounts.
import './lib/installPrompt'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)

// Production only: in dev, Vite serves unbundled modules that the worker's
// /assets/* cache would never match anyway, and a stale worker just confuses HMR.
if (import.meta.env.PROD && 'serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js').catch(() => {
      // Not fatal: the app works the same, just without the instant shell.
    })
  })
}
