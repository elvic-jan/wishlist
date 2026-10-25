import { useSyncExternalStore } from 'react'

/**
 * "Install app" support.
 *
 * Chrome, Edge and Samsung Internet announce that the app can be installed with
 * a `beforeinstallprompt` event, often before React has mounted, so this module
 * starts listening as soon as it's imported (main.tsx imports it first) and
 * keeps the event until the person asks to install. iPhone/iPad browsers have
 * no such event; there the app explains Share → Add to Home Screen instead.
 */

interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>
}

/**
 * - `prompt`: the browser can show its install dialog.
 * - `ios`: iPhone/iPad, where installing goes through the Share sheet.
 * - `installed`: already running as an installed app.
 * - `none`: nothing to offer (unsupported browser, or the browser hasn't offered yet).
 */
export type InstallOffer = 'prompt' | 'ios' | 'installed' | 'none'

let deferred: BeforeInstallPromptEvent | null = null
let justInstalled = false
const listeners = new Set<() => void>()
const notify = () => listeners.forEach(listener => listener())

if (typeof window !== 'undefined') {
  window.addEventListener('beforeinstallprompt', event => {
    // Keep the browser's own mini-infobar quiet; the menu offers it instead.
    event.preventDefault()
    deferred = event as BeforeInstallPromptEvent
    notify()
  })
  window.addEventListener('appinstalled', () => {
    deferred = null
    justInstalled = true
    notify()
  })
}

function runningInstalled(): boolean {
  return (
    window.matchMedia('(display-mode: standalone)').matches ||
    (navigator as Navigator & { standalone?: boolean }).standalone === true
  )
}

function isIos(): boolean {
  const ua = navigator.userAgent
  // iPadOS reports itself as a Mac; touch support gives it away.
  return /iPhone|iPad|iPod/.test(ua) || (/Macintosh/.test(ua) && navigator.maxTouchPoints > 1)
}

function currentOffer(): InstallOffer {
  if (justInstalled || runningInstalled()) return 'installed'
  if (deferred) return 'prompt'
  if (isIos()) return 'ios'
  return 'none'
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

/** What "Install app" can do in this browser right now. */
export function useInstallOffer(): InstallOffer {
  return useSyncExternalStore(subscribe, currentOffer, () => 'none')
}

/** Opens the browser's install dialog. Resolves to whether the person installed it. */
export async function promptInstall(): Promise<boolean> {
  const event = deferred
  if (!event) return false
  await event.prompt()
  const { outcome } = await event.userChoice
  // A prompt can be shown only once per event.
  deferred = null
  notify()
  return outcome === 'accepted'
}
