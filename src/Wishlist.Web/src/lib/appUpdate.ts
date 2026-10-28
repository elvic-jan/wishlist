import { useSyncExternalStore } from 'react'

// Finds out whether a newer version of the app is deployed than the one
// running. The deployed index.html names the entry script by its content hash
// (/assets/index-<hash>.js), so a different name there means a new release.

const MIN_INTERVAL_MS = 60_000
/** While the app is open, look again this often. */
const PERIODIC_MS = 15 * 60_000
/** After a failed check (offline, server waking up), retry after these delays, then every last one. */
const RETRY_DELAYS_MS = [10_000, 30_000, 60_000, 5 * 60_000]
const SW_TIMEOUT_MS = 5_000
/** A server waking from zero takes ~20 s; don't wait forever on one request. */
const FETCH_TIMEOUT_MS = 45_000

const MAX_NOTE_LENGTH = 200

let available = false
/** What the newer version brings (its release note), once one is known. */
let note: string | null = null
let lastCheck = 0
/** Whether the last check got no answer from the server. */
let lastFailed = false
let running: Promise<boolean> | null = null
const listeners = new Set<() => void>()

function setAvailable(releaseNote: string | null) {
  if (available) return
  available = true
  note = releaseNote
  listeners.forEach(l => l())
}

/** The page's <meta name="release-note">, trimmed; null when missing or empty. */
export function releaseNoteOf(html: string): string | null {
  const content = new DOMParser()
    .parseFromString(html, 'text/html')
    .querySelector('meta[name="release-note"]')
    ?.getAttribute('content')
    ?.trim()
  return content ? content.slice(0, MAX_NOTE_LENGTH) : null
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
    lastFailed = true
    try {
      const current = runningEntry()
      if (!current) {
        lastFailed = false // nothing to compare against; retrying won't help
        return false
      }
      const response = await fetch('/index.html', { cache: 'no-cache', signal: AbortSignal.timeout(FETCH_TIMEOUT_MS) })
      if (!response.ok) return false
      const html = await response.text()
      lastFailed = false
      const deployed = html.match(/\/assets\/index-[^"'\s]+\.js/)?.[0]
      if (deployed && deployed !== current) setAvailable(releaseNoteOf(html))
    } catch {
      // Offline, or the server is slow or waking up: keep the version we have;
      // startUpdateChecks tries again.
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

/**
 * Checks now, whenever the app comes back to the foreground or online, and
 * every so often while it's open. A check that got no answer (an installed app
 * resumed before its network is back, or a server still waking up) is retried
 * soon, not only on the next return to the foreground.
 */
export function startUpdateChecks() {
  if (!import.meta.env.PROD) return
  let timer: ReturnType<typeof setTimeout> | undefined
  let retries = 0

  const schedule = () => {
    clearTimeout(timer)
    if (available || document.visibilityState !== 'visible') return
    const delay = lastFailed ? RETRY_DELAYS_MS[Math.min(retries++, RETRY_DELAYS_MS.length - 1)] : PERIODIC_MS
    if (!lastFailed) retries = 0
    timer = setTimeout(() => check(true), delay)
  }

  const check = (force = false) => {
    if (!force && !lastFailed && Date.now() - lastCheck < MIN_INTERVAL_MS) return schedule()
    void checkForUpdate().then(schedule)
    navigator.serviceWorker
      ?.getRegistration()
      .then(r => r?.update())
      .catch(() => undefined)
  }

  check()
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') check()
    else clearTimeout(timer)
  })
  window.addEventListener('online', () => check(true))
  // Back from the browser's page cache (e.g. Android restoring the app).
  window.addEventListener('pageshow', e => {
    if (e.persisted) check()
  })
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

/** True once a newer version is known to be deployed. */
export function useUpdateAvailable(): boolean {
  return useSyncExternalStore(subscribe, () => available)
}

/** What the newer version brings, from its release note; null without one. */
export function useUpdateNote(): string | null {
  return useSyncExternalStore(subscribe, () => note)
}
