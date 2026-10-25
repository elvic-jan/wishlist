import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { WakingUp } from '@/components/WakingUp'
import { confirmEmail } from '@/api/passkeys'
import { useAuth } from '@/context/AuthContext'

/** Where the "Confirm your GiftCircles email" link leads. */
export function ConfirmEmailPage() {
  const [searchParams] = useSearchParams()
  const { user } = useAuth()
  const userId = searchParams.get('userId') ?? ''
  const token = searchParams.get('token') ?? ''
  const [state, setState] = useState<'confirming' | 'confirmed' | 'failed'>(
    userId && token ? 'confirming' : 'failed',
  )
  const sent = useRef(false)

  useEffect(() => {
    if (!userId || !token || sent.current) return
    sent.current = true
    confirmEmail(userId, token)
      .then(() => setState('confirmed'))
      .catch(() => setState('failed'))
  }, [userId, token])

  if (state === 'confirming') return <WakingUp fullScreen={false} />

  return (
    <Card>
      <CardHeader>
        <CardTitle>{state === 'confirmed' ? 'Email confirmed' : 'This link doesn’t work'}</CardTitle>
        <CardDescription>
          {state === 'confirmed'
            ? 'Thanks! If you ever lose your phone or can’t sign in, we can now help you get back in by email.'
            : 'The confirmation link is invalid or has expired. You can send a new one from your sign-in settings.'}
        </CardDescription>
      </CardHeader>
      <CardFooter>
        <Button asChild className="w-full">
          <Link to={user ? (state === 'confirmed' ? '/' : '/settings/security') : '/login'}>
            {user ? (state === 'confirmed' ? 'Go to my circles' : 'Open sign-in settings') : 'Sign in'}
          </Link>
        </Button>
      </CardFooter>
    </Card>
  )
}
