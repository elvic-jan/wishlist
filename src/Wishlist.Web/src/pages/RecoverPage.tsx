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
import { PasswordInput } from '@/components/ui/password-input'
import { Label } from '@/components/ui/label'
import { resetPassword } from '@/api/auth'
import { apiErrorMessage, recoverWithPasskey } from '@/api/passkeys'
import { useAuth } from '@/context/AuthContext'
import { Trans, useT } from '@/lib/i18n'
import { isPasskeyCancelled, passkeyErrorMessage, passkeysSupported } from '@/lib/webauthn'
import { MAX_PASSWORD } from '@/lib/inputLimits'

/**
 * Where the "Can't sign in?" email leads (`/recover`, and old `/reset-password`
 * links). The link lets the person add Face ID / fingerprint on this device,
 * or set a new password.
 */
export function RecoverPage() {
  const t = useT()
  const [searchParams] = useSearchParams()
  const email = searchParams.get('email') ?? ''
  const token = searchParams.get('token') ?? ''
  const canUsePasskeys = passkeysSupported()
  const [usePassword, setUsePassword] = useState(!canUsePasskeys)

  if (!email || !token) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>{t('recover.title')}</CardTitle>
        </CardHeader>
        <CardContent>
          <Alert variant="destructive">
            <AlertDescription>
              <Trans
                k="recover.missing"
                components={{ a: <Link to="/forgot-password" className="text-primary underline-offset-4 hover:underline" /> }}
              />
            </AlertDescription>
          </Alert>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{usePassword ? t('recover.newPassword.title') : t('recover.title')}</CardTitle>
        <CardDescription>
          <Trans k="recover.for" values={{ email }} components={{ b: <strong /> }} />
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
              {usePassword ? t('register.usePasskey') : t('recover.setPasswordInstead')}
            </button>
          </div>
        )}
      </CardContent>
      <CardFooter className="justify-center">
        <Link
          to="/login"
          className="text-sm text-primary underline-offset-4 hover:underline"
        >
          {t('common.backToSignIn')}
        </Link>
      </CardFooter>
    </Card>
  )
}

function PasskeyRecovery({ email, token }: { email: string; token: string }) {
  const t = useT()
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
        setError(t('recover.cancelled'))
      } else {
        setError(
          passkeyErrorMessage(err) ??
            apiErrorMessage(err) ??
            t('recover.failed'),
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
        {t('recover.passkeyText')}
      </p>
      <Button type="button" className="w-full" onClick={handleClick} disabled={busy}>
        <Fingerprint className="mr-2 h-4 w-4" aria-hidden />
        {busy ? t('common.waitingForDevice') : t('recover.usePasskey')}
      </Button>
    </div>
  )
}

function NewPasswordForm({ email, token }: { email: string; token: string }) {
  const t = useT()
  const navigate = useNavigate()
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')

    if (newPassword.length < 8) {
      setError(t('recover.tooShort'))
      return
    }
    if (newPassword !== confirmPassword) {
      setError(t('recover.mismatch'))
      return
    }

    setSubmitting(true)
    try {
      await resetPassword(email, token, newPassword)
      navigate('/login?reset=success')
    } catch {
      setError(t('recover.expired'))
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
          <Label htmlFor="reset-new-password">{t('recover.newPassword')}</Label>
          <PasswordInput
            id="reset-new-password"
            autoComplete="new-password"
            value={newPassword}
            onChange={e => setNewPassword(e.target.value)}
            maxLength={MAX_PASSWORD}
            required
            minLength={8}
          />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="reset-confirm-password">{t('recover.confirmPassword')}</Label>
          <PasswordInput
            id="reset-confirm-password"
            autoComplete="new-password"
            value={confirmPassword}
            onChange={e => setConfirmPassword(e.target.value)}
            maxLength={MAX_PASSWORD}
            required
            minLength={8}
          />
        </div>
        <Button type="submit" className="w-full" disabled={submitting}>
          {submitting ? t('recover.updating') : t('recover.setPassword')}
        </Button>
      </form>
    </>
  )
}
