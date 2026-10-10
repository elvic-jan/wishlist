import { useEffect, useState } from 'react'
import { Gift, Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'

/**
 * Shown while the first API call is pending. The backend scales to zero when
 * idle and takes ~20 s to start, so after a short grace period (a warm backend
 * answers well within it) we say so instead of showing a blank page.
 */
export function WakingUp({ graceMs = 1500, fullScreen = true }: { graceMs?: number; fullScreen?: boolean }) {
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    const timer = setTimeout(() => setVisible(true), graceMs)
    return () => clearTimeout(timer)
  }, [graceMs])

  if (!visible) return null

  return (
    <div
      role="status"
      aria-live="polite"
      className={cn(
        'flex flex-col items-center justify-center gap-4 px-4 text-center',
        fullScreen ? 'min-h-svh bg-background' : 'py-16',
      )}
    >
      <Gift className="h-8 w-8 text-primary" aria-hidden />
      <div className="flex items-center gap-2 text-sm font-medium text-foreground">
        <Loader2 className="h-4 w-4 animate-spin" aria-hidden />
        Waking up the server…
      </div>
      <p className="max-w-xs text-sm text-muted-foreground">
        It sleeps when nobody has visited for a while. This can take up to half a minute.
      </p>
    </div>
  )
}
