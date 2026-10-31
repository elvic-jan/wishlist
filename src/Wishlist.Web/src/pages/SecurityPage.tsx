import { useCallback, useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ArrowLeft, Check, Fingerprint, LogOut, Mail, Pencil, ShieldCheck, Trash2, X } from 'lucide-react'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { useToast } from '@/hooks/use-toast'
import {
  addPasskey,
  apiErrorMessage,
  getSecurityOverview,
  removePasskey,
  renamePasskey,
  resendEmailConfirmation,
  signOutOtherDevices,
  type PasskeySummary,
  type SecurityOverview,
} from '@/api/passkeys'
import { useAuth } from '@/context/AuthContext'
import { passkeyErrorMessage, passkeysSupported } from '@/lib/webauthn'

const dateFormat = new Intl.DateTimeFormat(undefined, { day: 'numeric', month: 'short', year: 'numeric' })

/** "Sign-in & security": passkeys, password, email confirmation and other devices. */
export function SecurityPage() {
  const { user } = useAuth()
  const { toast } = useToast()
  const [searchParams] = useSearchParams()
  const recovered = searchParams.get('recovered') === '1'
  const [overview, setOverview] = useState<SecurityOverview | null>(null)
  const [error, setError] = useState('')
  const [adding, setAdding] = useState(false)
  const [toRemove, setToRemove] = useState<PasskeySummary | null>(null)
  const [confirmSignOut, setConfirmSignOut] = useState(false)

  const load = useCallback(
    () =>
      getSecurityOverview()
        .then(setOverview)
        .catch(() => setError('Couldn’t load your sign-in settings.')),
    [],
  )

  useEffect(() => {
    let cancelled = false
    getSecurityOverview()
      .then(data => !cancelled && setOverview(data))
      .catch(() => !cancelled && setError('Couldn’t load your sign-in settings.'))
    return () => {
      cancelled = true
    }
  }, [])

  const handleAdd = async () => {
    setError('')
    setAdding(true)
    try {
      await addPasskey()
      await load()
      toast({ title: 'Face ID / fingerprint added', description: 'You can use it next time you sign in.' })
    } catch (err) {
      const message = passkeyErrorMessage(err) ?? apiErrorMessage(err)
      if (message) setError(message)
      else if (!(err instanceof DOMException)) setError('The passkey couldn’t be added. Please try again.')
    } finally {
      setAdding(false)
    }
  }

  const handleRemove = async () => {
    if (!toRemove) return
    setError('')
    try {
      await removePasskey(toRemove.id)
      await load()
      toast({ title: 'Passkey removed' })
    } catch (err) {
      setError(apiErrorMessage(err) ?? 'The passkey couldn’t be removed.')
    } finally {
      setToRemove(null)
    }
  }

  const handleSignOutOthers = async () => {
    setConfirmSignOut(false)
    try {
      await signOutOtherDevices()
      toast({ title: 'Signed out on other devices', description: 'This device stays signed in.' })
    } catch {
      setError('Couldn’t sign out other devices. Please try again.')
    }
  }

  const handleResend = async () => {
    try {
      await resendEmailConfirmation()
      toast({ title: 'Confirmation email sent', description: `Check the inbox of ${user?.email}.` })
    } catch {
      setError('Couldn’t send the email. Please try again later.')
    }
  }

  const onlyWayIn = overview ? !overview.hasPassword && overview.passkeys.length <= 1 : true

  return (
    <div className="space-y-6">
      <div>
        <Button asChild variant="ghost" size="sm" className="-ml-2 mb-2">
          <Link to="/">
            <ArrowLeft className="h-4 w-4" />
            Back
          </Link>
        </Button>
        <div className="flex items-center gap-2">
          <ShieldCheck className="h-5 w-5 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">Sign-in &amp; security</h1>
        </div>
      </div>

      {recovered && (
        <Alert className="border-primary/40">
          <AlertDescription>
            You&rsquo;re back in, and you&rsquo;ve been signed out on every other device.
            If you lost a phone or computer, remove its passkey below.
          </AlertDescription>
        </Alert>
      )}
      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Face ID &amp; fingerprint (passkeys)</CardTitle>
          <CardDescription>
            Sign in with Face ID, your fingerprint or your device&rsquo;s screen lock.
            Passkeys saved in iCloud Keychain, Google Password Manager or a password
            manager also work on your other devices.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          {!overview ? (
            <Skeleton className="h-12 w-full" />
          ) : overview.passkeys.length === 0 ? (
            <p className="text-sm text-muted-foreground">No passkeys yet.</p>
          ) : (
            <ul className="divide-y">
              {overview.passkeys.map(p => (
                <PasskeyRow
                  key={p.id}
                  passkey={p}
                  canRemove={!onlyWayIn}
                  onRemove={() => setToRemove(p)}
                  onRenamed={load}
                />
              ))}
            </ul>
          )}
          {passkeysSupported() ? (
            <Button type="button" variant="outline" className="w-full sm:w-auto" onClick={handleAdd} disabled={adding}>
              <Fingerprint className="mr-2 h-4 w-4" aria-hidden />
              {adding ? 'Waiting for your device…' : 'Add Face ID / fingerprint on this device'}
            </Button>
          ) : (
            <p className="text-sm text-muted-foreground">
              This browser can&rsquo;t create passkeys. Try on your phone, or a recent
              version of Chrome, Edge, Safari or Firefox.
            </p>
          )}
          {overview && onlyWayIn && overview.passkeys.length === 1 && (
            <p className="text-xs text-muted-foreground">
              This is your only way to sign in, so it can&rsquo;t be removed. Add
              another device first.
            </p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Password</CardTitle>
        </CardHeader>
        <CardContent className="text-sm text-muted-foreground">
          {!overview ? (
            <Skeleton className="h-5 w-48" />
          ) : overview.hasPassword ? (
            <p>You can also sign in with your password.</p>
          ) : (
            <p>
              You don&rsquo;t have a password; you sign in with Face ID or a fingerprint.
              If you&rsquo;d like one, use &ldquo;Can&rsquo;t sign in?&rdquo; on the sign-in
              page and choose &ldquo;Set a new password&rdquo;.
            </p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Email</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap items-center justify-between gap-3 text-sm">
          <div className="flex items-center gap-2">
            <Mail className="h-4 w-4 text-muted-foreground" aria-hidden />
            <span>{user?.email}</span>
            {overview &&
              (overview.emailConfirmed ? (
                <Badge variant="secondary">Confirmed</Badge>
              ) : (
                <Badge variant="outline">Not confirmed</Badge>
              ))}
          </div>
          {overview && !overview.emailConfirmed && (
            <Button type="button" variant="outline" size="sm" onClick={handleResend}>
              Send confirmation email
            </Button>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Other devices</CardTitle>
          <CardDescription>
            Lost a phone, or signed in on someone else&rsquo;s computer? Sign out
            everywhere except here.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button type="button" variant="outline" onClick={() => setConfirmSignOut(true)}>
            <LogOut className="mr-2 h-4 w-4" aria-hidden />
            Sign out all other devices
          </Button>
        </CardContent>
      </Card>

      <AlertDialog open={toRemove !== null} onOpenChange={open => !open && setToRemove(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove this passkey?</AlertDialogTitle>
            <AlertDialogDescription>
              &ldquo;{toRemove?.name ?? 'Passkey'}&rdquo; won&rsquo;t sign you in to
              Kadoozle any more. You can still delete it from your device&rsquo;s
              password manager afterwards.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleRemove}>Remove</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <AlertDialog open={confirmSignOut} onOpenChange={setConfirmSignOut}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Sign out all other devices?</AlertDialogTitle>
            <AlertDialogDescription>
              Everywhere else, you&rsquo;ll need Face ID, a fingerprint or your
              password to sign in again. This device stays signed in.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleSignOutOthers}>Sign out others</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

function PasskeyRow({
  passkey,
  canRemove,
  onRemove,
  onRenamed,
}: {
  passkey: PasskeySummary
  canRemove: boolean
  onRemove: () => void
  onRenamed: () => Promise<void>
}) {
  const [editing, setEditing] = useState(false)
  const [name, setName] = useState(passkey.name ?? '')
  const [saving, setSaving] = useState(false)

  const save = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!name.trim()) return
    setSaving(true)
    try {
      await renamePasskey(passkey.id, name.trim())
      await onRenamed()
      setEditing(false)
    } finally {
      setSaving(false)
    }
  }

  return (
    <li className="flex flex-wrap items-center justify-between gap-3 py-3">
      {editing ? (
        <form onSubmit={save} className="flex flex-1 items-center gap-2">
          <Input
            aria-label="Passkey name"
            value={name}
            maxLength={100}
            autoFocus
            onChange={e => setName(e.target.value)}
            className="h-8"
          />
          <Button type="submit" size="icon" variant="ghost" aria-label="Save name" disabled={saving}>
            <Check className="h-4 w-4" />
          </Button>
          <Button
            type="button"
            size="icon"
            variant="ghost"
            aria-label="Cancel renaming"
            onClick={() => {
              setName(passkey.name ?? '')
              setEditing(false)
            }}
          >
            <X className="h-4 w-4" />
          </Button>
        </form>
      ) : (
        <>
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <Fingerprint className="h-4 w-4 shrink-0 text-primary" aria-hidden />
              <span className="truncate text-sm font-medium">{passkey.name ?? 'Passkey'}</span>
              {passkey.isBackedUp && <Badge variant="secondary">Synced</Badge>}
            </div>
            <p className="mt-0.5 text-xs text-muted-foreground">
              Added {dateFormat.format(new Date(passkey.createdAt))}
            </p>
          </div>
          <div className="flex items-center gap-1">
            <Button type="button" size="icon" variant="ghost" aria-label="Rename passkey" onClick={() => setEditing(true)}>
              <Pencil className="h-4 w-4" />
            </Button>
            <Button
              type="button"
              size="icon"
              variant="ghost"
              aria-label="Remove passkey"
              disabled={!canRemove}
              onClick={onRemove}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        </>
      )}
    </li>
  )
}
