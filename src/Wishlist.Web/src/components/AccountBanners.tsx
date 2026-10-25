import { useEffect, useState } from 'react'
import { useLocation } from 'react-router-dom'
import { Fingerprint, Mail, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useToast } from '@/hooks/use-toast'
import { addPasskey, getSecurityOverview, resendEmailConfirmation, type SecurityOverview } from '@/api/passkeys'
import { useAuth } from '@/context/AuthContext'
import { isPasskeyCancelled, passkeyErrorMessage, passkeysSupported } from '@/lib/webauthn'

const dismissKey = (userId: string) => `giftcircles.passkeyPromptDismissed.${userId}`

function readDismissed(userId: string): boolean {
  try {
    return localStorage.getItem(dismissKey(userId)) === '1'
  } catch {
    return false
  }
}

function writeDismissed(userId: string) {
  try {
    localStorage.setItem(dismissKey(userId), '1')
  } catch {
    // Private mode or blocked storage: the prompt simply comes back next time.
  }
}

/**
 * At most one gentle nudge above the page: confirm the email address (so
 * recovery by email works), or, for accounts without a passkey, set up
 * Face ID / fingerprint. Hidden on the security page, which shows both.
 */
export function AccountBanners() {
  const { user } = useAuth()
  const { pathname } = useLocation()
  const { toast } = useToast()
  const [overview, setOverview] = useState<SecurityOverview | null>(null)
  const [hidden, setHidden] = useState(false)
  const [busy, setBusy] = useState(false)
  const onSecurityPage = pathname === '/settings/security'

  // Re-read when leaving the security page, where things may have changed.
  useEffect(() => {
    if (!user || onSecurityPage) return
    let cancelled = false
    getSecurityOverview()
      .then(data => !cancelled && setOverview(data))
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [user, onSecurityPage])

  if (!user || !overview || hidden || onSecurityPage) return null

  if (!overview.emailConfirmed) {
    return (
      <Banner icon={<Mail className="h-4 w-4" aria-hidden />} onClose={() => setHidden(true)}>
        <span>
          Please confirm your email address, so we can help you get back in if you
          ever lose your phone.
        </span>
        <Button
          size="sm"
          variant="outline"
          disabled={busy}
          onClick={async () => {
            setBusy(true)
            try {
              await resendEmailConfirmation()
              toast({ title: 'Confirmation email sent', description: `Check the inbox of ${user.email}.` })
              setHidden(true)
            } finally {
              setBusy(false)
            }
          }}
        >
          Send the email again
        </Button>
      </Banner>
    )
  }

  if (overview.passkeys.length === 0 && passkeysSupported() && !readDismissed(user.id)) {
    const dismiss = () => {
      writeDismissed(user.id)
      setHidden(true)
    }
    return (
      <Banner icon={<Fingerprint className="h-4 w-4" aria-hidden />} onClose={dismiss}>
        <span>Sign in faster next time with Face ID or your fingerprint.</span>
        <div className="flex gap-2">
          <Button
            size="sm"
            disabled={busy}
            onClick={async () => {
              setBusy(true)
              try {
                await addPasskey()
                toast({ title: 'All set', description: 'Next time, sign in with Face ID or your fingerprint.' })
                setHidden(true)
              } catch (err) {
                if (!isPasskeyCancelled(err)) {
                  toast({
                    title: 'That didn’t work',
                    description: passkeyErrorMessage(err) ?? 'You can try again from Sign-in & security.',
                  })
                }
              } finally {
                setBusy(false)
              }
            }}
          >
            {busy ? 'Waiting…' : 'Set up'}
          </Button>
          <Button size="sm" variant="ghost" onClick={dismiss}>
            Not now
          </Button>
        </div>
      </Banner>
    )
  }

  return null
}

function Banner({
  icon,
  onClose,
  children,
}: {
  icon: React.ReactNode
  onClose: () => void
  children: React.ReactNode
}) {
  return (
    <div className="mb-6 flex items-start gap-3 rounded-lg border border-primary/30 bg-primary/5 p-3 text-sm">
      <span className="mt-0.5 text-primary">{icon}</span>
      <div className="flex flex-1 flex-wrap items-center justify-between gap-3">{children}</div>
      <button
        type="button"
        aria-label="Dismiss"
        onClick={onClose}
        className="rounded p-0.5 text-muted-foreground hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      >
        <X className="h-4 w-4" />
      </button>
    </div>
  )
}
