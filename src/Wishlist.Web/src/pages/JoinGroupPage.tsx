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
import client from '@/api/client'
import { useAuth } from '@/context/AuthContext'

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
        const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
        setError(msg ?? 'Failed to join group. The link may have expired.')
        setStatus('error')
      })
  }, [code, user, navigate])

  if (restoring) return null

  if (status === 'unauthenticated') {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Sign in to join</CardTitle>
          <CardDescription>
            You need to be signed in before you can join a group.
          </CardDescription>
        </CardHeader>
        <CardFooter>
          <Button asChild className="w-full">
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
          <CardTitle>Joining group…</CardTitle>
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
