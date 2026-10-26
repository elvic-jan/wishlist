import { useEffect, useState } from 'react'
import { Megaphone, X } from 'lucide-react'
import { useLocation, useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import client from '@/api/client'
import { useAuth } from '@/context/AuthContext'

interface Message {
  id: string
  title: string
  text: string
  action?: { label: string; url: string } | null
}

// "Remind me next time" hides a message for this app session only: closing
// the tab or the installed app and opening it again shows it again.
const SNOOZE_KEY = 'giftcircles.snoozedMessages'

function readSnoozed(): string[] {
  try {
    return JSON.parse(sessionStorage.getItem(SNOOZE_KEY) ?? '[]') as string[]
  } catch {
    return []
  }
}

function snooze(id: string) {
  try {
    sessionStorage.setItem(SNOOZE_KEY, JSON.stringify([...new Set([...readSnoozed(), id])]))
  } catch {
    // Storage unavailable: the message just comes back on the next page load.
  }
}

/**
 * Lasting messages (what's new, the old-site import offer, …), one at a time.
 * Each can be acted on, put off until next time, or closed for good.
 */
export function MessageToaster({ hiddenOn = [] }: { hiddenOn?: string[] }) {
  const { user } = useAuth()
  const { pathname } = useLocation()
  const navigate = useNavigate()
  const [queue, setQueue] = useState<Message[]>([])

  useEffect(() => {
    if (!user) return
    let cancelled = false
    client
      .get<Message[]>('/messages')
      .then(r => {
        if (cancelled) return
        const snoozed = readSnoozed()
        setQueue(r.data.filter(m => !snoozed.includes(m.id)))
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [user])

  const current = queue[0]
  if (!current || hiddenOn.includes(pathname)) return null

  const next = () => setQueue(q => q.slice(1))
  const dismiss = (m: Message) => {
    client.post(`/messages/${encodeURIComponent(m.id)}/dismiss`).catch(() => undefined)
    next()
  }

  return (
    <div
      role="status"
      aria-live="polite"
      className="fixed inset-x-0 bottom-0 z-50 flex justify-center p-4 pb-[max(1rem,env(safe-area-inset-bottom))] sm:inset-x-auto sm:right-0 sm:justify-end"
    >
      <div className="w-full max-w-sm rounded-lg border bg-card p-4 text-card-foreground shadow-lg">
        <div className="flex items-start gap-3">
          <Megaphone className="mt-0.5 h-4 w-4 shrink-0 text-primary" aria-hidden />
          <div className="min-w-0 flex-1 space-y-1">
            <p className="text-sm font-semibold">{current.title}</p>
            <p className="text-sm text-muted-foreground">{current.text}</p>
          </div>
          <button
            type="button"
            onClick={() => dismiss(current)}
            className="-m-1 rounded-sm p-1 text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            aria-label="Close"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
        <div className="mt-3 flex flex-wrap justify-end gap-2">
          <Button
            size="sm"
            variant="ghost"
            onClick={() => {
              snooze(current.id)
              next()
            }}
          >
            Remind me next time
          </Button>
          {current.action && (
            <Button
              size="sm"
              onClick={() => {
                const url = current.action!.url
                dismiss(current)
                navigate(url)
              }}
            >
              {current.action.label}
            </Button>
          )}
        </div>
      </div>
    </div>
  )
}
