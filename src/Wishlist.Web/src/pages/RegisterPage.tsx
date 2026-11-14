import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
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
import { PasswordInput } from '@/components/ui/password-input'
import { Label } from '@/components/ui/label'
import { InvalidInvitation } from '@/components/InvalidInvitation'
import { WakingUp } from '@/components/WakingUp'
import { Fingerprint } from 'lucide-react'
import { signUpWithPasskey } from '@/api/passkeys'
import { useAuth } from '@/context/AuthContext'
import { Trans, useT } from '@/lib/i18n'
import { inviteErrorMessage } from '@/lib/inviteErrors'
import { useInvitationCheck } from '@/lib/useInvitationCheck'
import { isPasskeyCancelled, passkeyErrorMessage, passkeysSupported } from '@/lib/webauthn'
import { MAX_DISPLAY_NAME, MAX_EMAIL, MAX_PASSWORD } from '@/lib/inputLimits'

export function RegisterPage() {
  const [searchParams] = useSearchParams()
  const inviteCode = searchParams.get('invite') ?? undefined
  // Sign-up always needs an invitation.
  return inviteCode ? <InvitedRegistration inviteCode={inviteCode} /> : <InvitationOnly />
}

/** Checks the code before showing the form, so a dead link is reported up front. */
function InvitedRegistration({ inviteCode }: { inviteCode: string }) {
  const invitation = useInvitationCheck(inviteCode)

  if (invitation.state === 'checking') return <WakingUp fullScreen={false} />
  if (invitation.state === 'invalid') return <InvalidInvitation message={invitation.message} />
  return <RegisterForm inviteCode={inviteCode} />
}

function InvitationOnly() {
  const t = useT()
  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('register.invitationOnly.title')}</CardTitle>
        <CardDescription>
          {t('register.invitationOnly.text')}
        </CardDescription>
      </CardHeader>
      <CardFooter>
        <Button asChild variant="outline" className="w-full">
          <Link to="/login">{t('login.title')}</Link>
        </Button>
      </CardFooter>
    </Card>
  )
}

function RegisterForm({ inviteCode }: { inviteCode: string }) {
  const t = useT()
  const { register, startSession } = useAuth()
  const navigate = useNavigate()
  const canUsePasskeys = passkeysSupported()
  const [usePassword, setUsePassword] = useState(!canUsePasskeys)
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const goToCircle = (joinedGroupId: string) => navigate(`/circles/${joinedGroupId}`)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      if (usePassword) {
        goToCircle(await register(email, displayName, password, inviteCode))
      } else {
        const result = await signUpWithPasskey(email, displayName, inviteCode)
        startSession(result)
        goToCircle(result.joinedGroupId)
      }
    } catch (err: unknown) {
      if (!usePassword && isPasskeyCancelled(err)) {
        setError(t('register.cancelled'))
      } else {
        setError(passkeyErrorMessage(err) ?? inviteErrorMessage(err, t('register.failed')))
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('register.title')}</CardTitle>
        <CardDescription>
          {t('register.description')}
        </CardDescription>
      </CardHeader>
      <CardContent>
        {error && (
          <Alert variant="destructive" className="mb-4">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="register-display-name">{t('register.displayName')}</Label>
            <Input
              id="register-display-name"
              autoComplete="name"
              value={displayName}
              onChange={e => setDisplayName(e.target.value)}
              maxLength={MAX_DISPLAY_NAME}
              required
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="register-email">{t('common.email')}</Label>
            <Input
              id="register-email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              maxLength={MAX_EMAIL}
              required
            />
          </div>
          {usePassword ? (
            <>
              <div className="space-y-1.5">
                <Label htmlFor="register-password">{t('common.password')}</Label>
                <PasswordInput
                  id="register-password"
                  autoComplete="new-password"
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  maxLength={MAX_PASSWORD}
                  required
                  minLength={8}
                />
                <p className="text-xs text-muted-foreground">
                  {t('register.passwordHint')}
                </p>
              </div>
              <Button type="submit" className="w-full" disabled={submitting}>
                {submitting ? t('register.creating') : t('register.title')}
              </Button>
            </>
          ) : (
            <>
              <Button type="submit" className="w-full" disabled={submitting}>
                <Fingerprint className="mr-2 h-4 w-4" aria-hidden />
                {submitting ? t('common.waitingForDevice') : t('register.withPasskey')}
              </Button>
              <p className="text-xs text-muted-foreground">
                {t('register.passkeyHint')}
              </p>
            </>
          )}
        </form>
        {canUsePasskeys && (
          <div className="mt-4 text-center text-sm">
            <button
              type="button"
              className="text-primary underline-offset-4 hover:underline"
              onClick={() => {
                setError('')
                setUsePassword(p => !p)
              }}
            >
              {usePassword ? t('register.usePasskey') : t('register.usePassword')}
            </button>
          </div>
        )}
      </CardContent>
      <CardFooter className="justify-center">
        <p className="text-sm text-muted-foreground">
          <Trans
            k="register.haveAccount"
            components={{
              a: (
                <Link
                  to={inviteCode ? `/login?redirect=/join/${inviteCode}` : '/login'}
                  className="text-primary underline-offset-4 hover:underline"
                />
              ),
            }}
          />
        </p>
      </CardFooter>
    </Card>
  )
}
