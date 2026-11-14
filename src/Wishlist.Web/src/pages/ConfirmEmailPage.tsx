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
import { useT } from '@/lib/i18n'

/** Where the "Confirm your Kadoozle email" link leads. */
export function ConfirmEmailPage() {
  const t = useT()
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
        <CardTitle>{state === 'confirmed' ? t('confirmEmail.done.title') : t('confirmEmail.failed.title')}</CardTitle>
        <CardDescription>
          {state === 'confirmed'
            ? t('confirmEmail.done.text')
            : t('confirmEmail.failed.text')}
        </CardDescription>
      </CardHeader>
      <CardFooter>
        <Button asChild className="w-full">
          <Link to={user ? (state === 'confirmed' ? '/' : '/settings/security') : '/login'}>
            {user ? (state === 'confirmed' ? t('confirmEmail.goToCircles') : t('confirmEmail.openSettings')) : t('login.title')}
          </Link>
        </Button>
      </CardFooter>
    </Card>
  )
}
