import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Fingerprint } from 'lucide-react'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { resetPassword } from '@/api/auth'
import { apiErrorMessage, recoverWithPasskey } from '@/api/passkeys'
import { useAuth } from '@/context/AuthContext'
import { isPasskeyCancelled, passkeyErrorMessage, passkeysSupported } from '@/lib/webauthn'

/**
 * Where the "Can't sign in?" email leads (`/recover`, and old `/reset-password`
 * links). The link lets the person add Face ID / fingerprint on this device,
 * or set a new password.
 */
export function RecoverPage() {
  const [searchParams] = useSearchParams()
  const email = searchParams.get('email') ?? ''
  const token = searchParams.get('token') ?? ''
  const canUsePasskeys = passkeysSupported()
  const [usePassword, setUsePassword] = useState(!canUsePasskeys)

  if (!email || !token) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Get back into your account</CardTitle>
        </CardHeader>
        <CardContent>
          <Alert variant="destructive">
            <AlertDescription>
              This link is missing required information. Please request a new one
              from the{' '}
              <Link
                to="/forgot-password"
                className="text-primary underline-offset-4 hover:underline"
              >
                Can&rsquo;t sign in?
              </Link>{' '}
              page.
            </AlertDescription>
          </Alert>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{usePassword ? 'Choose a new password' : 'Get back into your account'}</CardTitle>
        <CardDescription>
          For <strong>{email}</strong>.
        </CardDescription>
      </CardHeader>
      <CardContent>
        {usePassword ? (
          <NewPasswordForm email={email} token={token} />
        ) : (
          <PasskeyRecovery email={email} token={token} />
        )}
        {canUsePasskeys && (
          <div className="mt-4 text-center text-sm">
            <button
              type="button"
              className="text-primary underline-offset-4 hover:underline"
              onClick={() => setUsePassword(p => !p)}
            >
              {usePassword ? 'Use Face ID / fingerprint instead' : 'Set a new password instead'}
            </button>
          </div>
        )}
      </CardContent>
      <CardFooter className="justify-center">
        <Link
          to="/login"
          className="text-sm text-primary underline-offset-4 hover:underline"
        >
          Back to sign in
        </Link>
      </CardFooter>
    </Card>
  )
}

function PasskeyRecovery({ email, token }: { email: string; token: string }) {
  const { startSession } = useAuth()
  const navigate = useNavigate()
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const handleClick = async () => {
    setError('')
    setBusy(true)
    try {
      startSession(await recoverWithPasskey(email, token))
      navigate('/settings/security?recovered=1')
    } catch (err) {
      if (isPasskeyCancelled(err)) {
        setError('Face ID / fingerprint was cancelled. Try again, or set a new password instead.')
      } else {
        setError(
          passkeyErrorMessage(err) ??
            apiErrorMessage(err) ??
            'Something went wrong. Please try again.',
        )
      }
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="space-y-4">
      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}
      <p className="text-sm text-muted-foreground">
        Set up Face ID, your fingerprint or this device&rsquo;s screen lock to sign
        in from now on. For your safety, you&rsquo;ll be signed out on every other
        device.
      </p>
      <Button type="button" className="w-full" onClick={handleClick} disabled={busy}>
        <Fingerprint className="mr-2 h-4 w-4" aria-hidden />
        {busy ? 'Waiting for your device…' : 'Use Face ID / fingerprint on this device'}
      </Button>
    </div>
  )
}

function NewPasswordForm({ email, token }: { email: string; token: string }) {
  const navigate = useNavigate()
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')

    if (newPassword.length < 8) {
      setError('Password must be at least 8 characters.')
      return
    }
    if (newPassword !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }

    setSubmitting(true)
    try {
      await resetPassword(email, token, newPassword)
      navigate('/login?reset=success')
    } catch {
      setError('This link is invalid or has expired. Please request a new one.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <>
      {error && (
        <Alert variant="destructive" className="mb-4">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}
      <form onSubmit={handleSubmit} className="space-y-4">
        <input type="email" name="username" autoComplete="username" value={email} readOnly hidden />
        <div className="space-y-1.5">
          <Label htmlFor="reset-new-password">New password</Label>
          <Input
            id="reset-new-password"
            type="password"
            autoComplete="new-password"
            value={newPassword}
            onChange={e => setNewPassword(e.target.value)}
            required
            minLength={8}
          />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="reset-confirm-password">Confirm password</Label>
          <Input
            id="reset-confirm-password"
            type="password"
            autoComplete="new-password"
            value={confirmPassword}
            onChange={e => setConfirmPassword(e.target.value)}
            required
            minLength={8}
          />
        </div>
        <Button type="submit" className="w-full" disabled={submitting}>
          {submitting ? 'Updating…' : 'Set new password'}
        </Button>
      </form>
    </>
  )
}
