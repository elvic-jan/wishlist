import { useEffect, useState } from 'react'
import { Megaphone } from 'lucide-react'
import { useLocation, useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import client from '@/api/client'
import { useAuth } from '@/context/AuthContext'
import { LastingToast } from '@/components/LastingToast'
import { applyUpdate, checkForUpdateWithin, useUpdateAvailable } from '@/lib/appUpdate'

interface Message {
  id: string
  title: string
  text: string
  action?: { label: string; url: string } | null
}

// "Remind me next time" hides a message for this app session only: closing
// the tab or the installed app and opening it again shows it again.
const SNOOZE_KEY = 'giftcircles.snoozedMessages'

// How long an action waits for the new-version check before navigating anyway.
const ACTION_CHECK_MS = 3_000

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
  // Messages are about the newest version, so they wait until it's loaded.
  const updateAvailable = useUpdateAvailable()

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
  if (!current || updateAvailable || hiddenOn.includes(pathname)) return null

  const next = () => setQueue(q => q.slice(1))
  const dismiss = (m: Message) => {
    client.post(`/messages/${encodeURIComponent(m.id)}/dismiss`).catch(() => undefined)
    next()
  }

  return (
    <LastingToast icon={Megaphone} title={current.title} text={current.text} onClose={() => dismiss(current)}>
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
          onClick={async () => {
            const url = current.action!.url
            dismiss(current)
            // An app older than the server may not have the page this message
            // is about yet, so open it in the new version when there is one.
            if (await checkForUpdateWithin(ACTION_CHECK_MS)) void applyUpdate(url)
            else navigate(url)
          }}
        >
          {current.action.label}
        </Button>
      )}
    </LastingToast>
  )
}
