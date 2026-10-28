import { lazy, type ComponentType } from 'react'
import { applyUpdate } from '@/lib/appUpdate'

const RELOADED_AT = 'giftcircles-chunk-reload'
const RELOAD_WINDOW_MS = 10_000

/**
 * A page that loads its code the first time it's opened, so the first visit
 * downloads less. If the code can't be loaded (a tab opened before a release,
 * whose files are gone from the server), load the new version once.
 */
export function lazyPage<M, K extends keyof M>(load: () => Promise<M>, name: K) {
  return lazy(async () => {
    try {
      const module = await load()
      return { default: module[name] as ComponentType }
    } catch (err) {
      if (!reloadedRecently()) {
        markReload()
        await applyUpdate()
        return new Promise<never>(() => {}) // the page is reloading
      }
      throw err
    }
  })
}

function reloadedRecently(): boolean {
  try {
    return Date.now() - Number(sessionStorage.getItem(RELOADED_AT) ?? 0) < RELOAD_WINDOW_MS
  } catch {
    return false
  }
}

function markReload() {
  try {
    sessionStorage.setItem(RELOADED_AT, String(Date.now()))
  } catch {
    // Storage blocked: still reload once; a second failure shows the error.
  }
}
