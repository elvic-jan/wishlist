import { useSyncExternalStore } from 'react'

// Finds out whether a newer version of the app is deployed than the one
// running. The deployed index.html names the entry script by its content hash
// (/assets/index-<hash>.js), so a different name there means a new release.

const MIN_INTERVAL_MS = 60_000
const SW_TIMEOUT_MS = 5_000

let available = false
let lastCheck = 0
let running: Promise<boolean> | null = null
const listeners = new Set<() => void>()

function setAvailable() {
  if (available) return
  available = true
  listeners.forEach(l => l())
}

function runningEntry(): string | null {
  return document.querySelector<HTMLScriptElement>('script[type="module"][src*="/assets/"]')?.getAttribute('src') ?? null
}

/**
 * Checks the server (reusing a check that's already running) and resolves to
 * whether a newer version is deployed. Failures resolve to the last known answer.
 */
export function checkForUpdate(): Promise<boolean> {
  if (!import.meta.env.PROD || available) return Promise.resolve(available)
  if (running) return running

  lastCheck = Date.now()
  running = (async () => {
    try {
      const current = runningEntry()
      if (!current) return false
      const response = await fetch('/index.html', { cache: 'no-cache' })
      if (!response.ok) return false
      const deployed = (await response.text()).match(/\/assets\/index-[^"'\s]+\.js/)?.[0]
      if (deployed && deployed !== current) setAvailable()
    } catch {
      // Unreachable or slow server: keep the version we have, try again later.
    } finally {
      running = null
    }
    return available
  })()
  return running
}

/** Like checkForUpdate, but gives up waiting after `ms` (the check keeps going). */
export function checkForUpdateWithin(ms: number): Promise<boolean> {
  return Promise.race([checkForUpdate(), new Promise<boolean>(r => setTimeout(() => r(available), ms))])
}

/**
 * Loads the new version: asks the service worker to cache the new shell first
 * (otherwise it would serve the old one again), then reloads or goes to `url`.
 */
export async function applyUpdate(url?: string) {
  const worker = navigator.serviceWorker?.controller
  if (worker) {
    await new Promise<void>(resolve => {
      const channel = new MessageChannel()
      const timer = setTimeout(resolve, SW_TIMEOUT_MS)
      channel.port1.onmessage = () => {
        clearTimeout(timer)
        resolve()
      }
      worker.postMessage({ type: 'refresh-shell' }, [channel.port2])
    })
  }
  if (url) window.location.assign(url)
  else window.location.reload()
}

/** Checks now and whenever the app comes back to the foreground. */
export function startUpdateChecks() {
  if (!import.meta.env.PROD) return
  const check = () => {
    if (Date.now() - lastCheck < MIN_INTERVAL_MS) return
    void checkForUpdate()
    navigator.serviceWorker
      ?.getRegistration()
      .then(r => r?.update())
      .catch(() => undefined)
  }
  check()
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') check()
  })
}

/** True once a newer version is known to be deployed. */
export function useUpdateAvailable(): boolean {
  return useSyncExternalStore(
    l => {
      listeners.add(l)
      return () => listeners.delete(l)
    },
    () => available,
  )
}
