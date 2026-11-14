import { useEffect, useState } from 'react'
import { useT } from '@/lib/i18n'

// A test environment (staging) names itself in /healthz; production doesn't.
// /healthz is never cached by the service worker and needs no database. The
// label is remembered so the banner is there at once on the next visit, even
// while the server is still waking up.
const STORAGE_KEY = 'environmentLabel'
const FETCH_TIMEOUT_MS = 45_000

function rememberedLabel(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEY)
  } catch {
    return null
  }
}

function remember(label: string | null) {
  try {
    if (label) localStorage.setItem(STORAGE_KEY, label)
    else localStorage.removeItem(STORAGE_KEY)
  } catch {
    // Storage unavailable (privacy mode): the banner still shows once /healthz answers.
  }
}

/** A banner on every page of a test environment, so it's never mistaken for the real app. Can't be dismissed. */
export function EnvironmentBanner() {
  const t = useT()
  const [label, setLabel] = useState<string | null>(rememberedLabel)

  useEffect(() => {
    let cancelled = false
    fetch('/healthz', { cache: 'no-store', signal: AbortSignal.timeout(FETCH_TIMEOUT_MS) })
      .then(response => (response.ok ? response.json() : null))
      .then((health: { environment?: unknown } | null) => {
        if (cancelled || !health) return
        const next = typeof health.environment === 'string' && health.environment ? health.environment : null
        remember(next)
        setLabel(next)
      })
      .catch(() => {
        // Offline or still waking up: keep what we remembered.
      })
    return () => {
      cancelled = true
    }
  }, [])

  if (!label) return null

  return (
    <div
      role="note"
      className="bg-amber-400 px-4 py-1 text-center text-xs font-semibold uppercase tracking-wide text-black"
    >
      {t('environment.banner', { label })}
    </div>
  )
}
