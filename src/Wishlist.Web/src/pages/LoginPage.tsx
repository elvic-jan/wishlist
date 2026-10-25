import { useCallback, useEffect, useRef, useState } from 'react'
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
import { Separator } from '@/components/ui/separator'
import { apiErrorMessage, signInWithPasskey } from '@/api/passkeys'
import { useAuth } from '@/context/AuthContext'
import {
  isPasskeyCancelled,
  passkeyAutofillSupported,
  passkeyErrorMessage,
  passkeysSupported,
} from '@/lib/webauthn'

/** Only same-site paths, so a crafted link can't send people elsewhere after sign-in. */
function safeRedirect(value: string | null): string {
  return value && value.startsWith('/') && !value.startsWith('//') ? value : '/'
}

export function LoginPage() {
  const { login, startSession } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [passkeyBusy, setPasskeyBusy] = useState(false)
  const autofill = useRef<AbortController | null>(null)

  const resetSuccess = searchParams.get('reset') === 'success'
  const redirect = safeRedirect(searchParams.get('redirect'))
  const canUsePasskeys = passkeysSupported()

  const finishPasskeySignIn = useCallback(
    (result: Awaited<ReturnType<typeof signInWithPasskey>>) => {
      startSession(result)
      navigate(redirect)
    },
    [navigate, redirect, startSession],
  )

  const passkeyFailed = useCallback((err: unknown) => {
    if (isPasskeyCancelled(err)) return
    setError(
      passkeyErrorMessage(err) ??
        (apiErrorMessage(err)
          ? 'That passkey isn’t linked to a GiftCircles account. Sign in with your password, or use “Can’t sign in?”.'
          : 'Signing in with Face ID / fingerprint didn’t work. Please try again.'),
    )
  }, [])

  // Offer saved passkeys in the email field's autofill, without a dialog.
  const startAutofill = useCallback(() => {
    autofill.current?.abort()
    const controller = new AbortController()
    autofill.current = controller
    passkeyAutofillSupported()
      .then(supported =>
        supported && !controller.signal.aborted
          ? signInWithPasskey({ conditional: true, signal: controller.signal }).then(finishPasskeySignIn)
          : undefined,
      )
      .catch(err => {
        if (!controller.signal.aborted) passkeyFailed(err)
      })
    return controller
  }, [finishPasskeySignIn, passkeyFailed])

  useEffect(() => {
    const controller = startAutofill()
    return () => controller.abort()
  }, [startAutofill])

  const handlePasskey = async () => {
    setError('')
    autofill.current?.abort()
    setPasskeyBusy(true)
    try {
      finishPasskeySignIn(await signInWithPasskey())
    } catch (err) {
      passkeyFailed(err)
      startAutofill()
    } finally {
      setPasskeyBusy(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      await login(email, password)
      navigate(redirect)
    } catch {
      setError('Invalid email or password.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Sign in</CardTitle>
        <CardDescription>
          Welcome back. Sign in to see your circles and wishlists.
        </CardDescription>
      </CardHeader>
      <CardContent>
        {resetSuccess && (
          <Alert className="mb-4 border-primary/40">
            <AlertDescription>
              Password updated. Please sign in with your new password.
            </AlertDescription>
          </Alert>
        )}
        {error && (
          <Alert variant="destructive" className="mb-4">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        {canUsePasskeys && (
          <>
            <Button
              type="button"
              className="w-full"
              onClick={handlePasskey}
              disabled={passkeyBusy}
            >
              <Fingerprint className="mr-2 h-4 w-4" aria-hidden />
              {passkeyBusy ? 'Waiting for your device…' : 'Sign in with Face ID / fingerprint'}
            </Button>
            <div className="my-5 flex items-center gap-3">
              <Separator className="flex-1" />
              <span className="text-xs text-muted-foreground">or use your password</span>
              <Separator className="flex-1" />
            </div>
          </>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="login-email">Email</Label>
            <Input
              id="login-email"
              type="email"
              autoComplete="username webauthn"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="login-password">Password</Label>
            <Input
              id="login-password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
            />
          </div>
          <Button
            type="submit"
            variant={canUsePasskeys ? 'outline' : 'default'}
            className="w-full"
            disabled={submitting}
          >
            {submitting ? 'Signing in…' : 'Sign in with password'}
          </Button>
        </form>
        <div className="mt-4 text-center text-sm">
          <Link
            to="/forgot-password"
            className="text-primary underline-offset-4 hover:underline"
          >
            Can’t sign in?
          </Link>
        </div>
      </CardContent>
      <CardFooter className="justify-center">
        <p className="text-sm text-muted-foreground">
          New here? Ask a family member for an invite link.
        </p>
      </CardFooter>
    </Card>
  )
}
