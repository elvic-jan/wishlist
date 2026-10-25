import { useEffect, useState } from 'react'
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
import { Label } from '@/components/ui/label'
import { InvalidInvitation } from '@/components/InvalidInvitation'
import { WakingUp } from '@/components/WakingUp'
import { Fingerprint } from 'lucide-react'
import client from '@/api/client'
import { signUpWithPasskey } from '@/api/passkeys'
import { useAuth } from '@/context/AuthContext'
import { inviteErrorMessage } from '@/lib/inviteErrors'
import { useInvitationCheck } from '@/lib/useInvitationCheck'
import { isPasskeyCancelled, passkeyErrorMessage, passkeysSupported } from '@/lib/webauthn'

export function RegisterPage() {
  const [searchParams] = useSearchParams()
  const inviteCode = searchParams.get('invite') ?? undefined
  return inviteCode ? <InvitedRegistration inviteCode={inviteCode} /> : <CodelessRegistration />
}

/** Checks the code before showing the form, so a dead link is reported up front. */
function InvitedRegistration({ inviteCode }: { inviteCode: string }) {
  const invitation = useInvitationCheck(inviteCode)

  if (invitation.state === 'checking') return <WakingUp fullScreen={false} />
  if (invitation.state === 'invalid') return <InvalidInvitation message={invitation.message} />
  return <RegisterForm inviteCode={inviteCode} />
}

/**
 * Without a code the form is only offered while the system has no accounts yet
 * (first-account bootstrap); otherwise sign-up is by invitation only.
 */
function CodelessRegistration() {
  const [inviteRequired, setInviteRequired] = useState<boolean | null>(null)

  useEffect(() => {
    client
      .get<{ inviteRequired: boolean }>('/auth/registration')
      .then(({ data }) => setInviteRequired(data.inviteRequired))
      .catch(() => setInviteRequired(true))
  }, [])

  if (inviteRequired === null) return <WakingUp fullScreen={false} />
  if (inviteRequired) return <InvitationOnly />
  return <RegisterForm />
}

function InvitationOnly() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Invitation only</CardTitle>
        <CardDescription>
          Accounts are created from an invite link. Ask a family member to send
          you one from their circle page.
        </CardDescription>
      </CardHeader>
      <CardFooter>
        <Button asChild variant="outline" className="w-full">
          <Link to="/login">Sign in</Link>
        </Button>
      </CardFooter>
    </Card>
  )
}

function RegisterForm({ inviteCode }: { inviteCode?: string }) {
  const { register, startSession } = useAuth()
  const navigate = useNavigate()
  const canUsePasskeys = passkeysSupported()
  const [usePassword, setUsePassword] = useState(!canUsePasskeys)
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const goToCircle = (joinedGroupId?: string | null) =>
    navigate(joinedGroupId ? `/circles/${joinedGroupId}` : '/')

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
        setError('Face ID / fingerprint was cancelled. Try again, or use a password instead.')
      } else {
        setError(passkeyErrorMessage(err) ?? inviteErrorMessage(err, 'Registration failed.'))
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Create account</CardTitle>
        <CardDescription>
          {inviteCode
            ? 'You’ve been invited to join a circle. Create your account to accept.'
            : 'Set up your account to share wishlists with friends and family.'}
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
            <Label htmlFor="register-display-name">Display name</Label>
            <Input
              id="register-display-name"
              autoComplete="name"
              value={displayName}
              onChange={e => setDisplayName(e.target.value)}
              required
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="register-email">Email</Label>
            <Input
              id="register-email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
            />
          </div>
          {usePassword ? (
            <>
              <div className="space-y-1.5">
                <Label htmlFor="register-password">Password</Label>
                <Input
                  id="register-password"
                  type="password"
                  autoComplete="new-password"
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  required
                  minLength={8}
                />
                <p className="text-xs text-muted-foreground">
                  At least 8 characters.
                </p>
              </div>
              <Button type="submit" className="w-full" disabled={submitting}>
                {submitting ? 'Creating account…' : 'Create account'}
              </Button>
            </>
          ) : (
            <>
              <Button type="submit" className="w-full" disabled={submitting}>
                <Fingerprint className="mr-2 h-4 w-4" aria-hidden />
                {submitting ? 'Waiting for your device…' : 'Create account with Face ID / fingerprint'}
              </Button>
              <p className="text-xs text-muted-foreground">
                Your phone or computer will ask for Face ID, your fingerprint or
                its screen lock. No password to remember.
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
              {usePassword ? 'Use Face ID / fingerprint instead' : 'Use a password instead'}
            </button>
          </div>
        )}
      </CardContent>
      <CardFooter className="justify-center">
        <p className="text-sm text-muted-foreground">
          Already have an account?{' '}
          <Link
            to={inviteCode ? `/login?redirect=/join/${inviteCode}` : '/login'}
            className="text-primary underline-offset-4 hover:underline"
          >
            Sign in
          </Link>
        </p>
      </CardFooter>
    </Card>
  )
}
