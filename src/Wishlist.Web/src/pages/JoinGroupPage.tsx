import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
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
import { InvalidInvitation } from '@/components/InvalidInvitation'
import { WakingUp } from '@/components/WakingUp'
import client from '@/api/client'
import { useAuth } from '@/context/AuthContext'
import { inviteErrorMessage } from '@/lib/inviteErrors'
import { useInvitationCheck } from '@/lib/useInvitationCheck'

type JoinStatus = 'joining' | 'done' | 'error' | 'unauthenticated'

export function JoinGroupPage() {
  const { code } = useParams<{ code: string }>()
  const { user, restoring } = useAuth()
  const navigate = useNavigate()
  const [joinStatus, setStatus] = useState<Exclude<JoinStatus, 'unauthenticated'>>('joining')
  // Derived from auth state so we don't have to setState inside the effect
  // just to reach the 'unauthenticated' branch.
  const status: JoinStatus = user ? joinStatus : 'unauthenticated'
  const [error, setError] = useState('')

  useEffect(() => {
    if (!user) return

    client
      .post(`/invitations/${code}/join`)
      .then(() => {
        setStatus('done')
        setTimeout(() => navigate('/'), 2000)
      })
      .catch((err: unknown) => {
        setError(inviteErrorMessage(err, 'Failed to join circle. The link may have expired.'))
        setStatus('error')
      })
  }, [code, user, navigate])

  // Signed-out visitors get the code checked before they're offered sign-up;
  // signed-in users find out from the join call itself.
  const invitation = useInvitationCheck(!restoring && !user ? code : undefined)

  if (restoring) return <WakingUp fullScreen={false} />

  if (status === 'unauthenticated') {
    if (invitation.state === 'checking') return <WakingUp fullScreen={false} />
    if (invitation.state === 'invalid') return <InvalidInvitation message={invitation.message} />
    return (
      <Card>
        <CardHeader>
          <CardTitle>You&rsquo;ve been invited</CardTitle>
          <CardDescription>
            Create an account to join the circle, or sign in if you already have one.
          </CardDescription>
        </CardHeader>
        <CardFooter className="flex-col gap-2">
          <Button asChild className="w-full">
            <Link to={`/register?invite=${code}`}>Create account</Link>
          </Button>
          <Button asChild variant="outline" className="w-full">
            <Link to={`/login?redirect=/join/${code}`}>Sign in</Link>
          </Button>
        </CardFooter>
      </Card>
    )
  }

  if (status === 'joining') {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Joining circle…</CardTitle>
          <CardDescription>This will only take a moment.</CardDescription>
        </CardHeader>
      </Card>
    )
  }

  if (status === 'done') {
    return (
      <Card>
        <CardHeader>
          <CardTitle>You&rsquo;re in!</CardTitle>
          <CardDescription>Taking you to the home page…</CardDescription>
        </CardHeader>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Couldn&rsquo;t join</CardTitle>
      </CardHeader>
      <CardContent>
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      </CardContent>
      <CardFooter className="justify-center">
        <Button asChild variant="outline">
          <Link to="/">Go home</Link>
        </Button>
      </CardFooter>
    </Card>
  )
}
