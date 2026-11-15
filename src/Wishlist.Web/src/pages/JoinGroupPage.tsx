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
import { Trans, useT } from '@/lib/i18n'
import { inviteErrorMessage } from '@/lib/inviteErrors'
import { useInvitationCheck, type ValidInvitation } from '@/lib/useInvitationCheck'

type JoinStatus = 'idle' | 'joining' | 'done' | 'declined' | 'error'

/**
 * An invite link. A circle's link joins straight away when signed in; a
 * personal Secret Santa invitation asks Join or Decline first, and can be
 * declined without an account.
 */
export function JoinGroupPage() {
  const t = useT()
  const { code } = useParams<{ code: string }>()
  const { user, restoring } = useAuth()
  const navigate = useNavigate()
  const [status, setStatus] = useState<JoinStatus>('idle')
  const [error, setError] = useState('')

  const invitation = useInvitationCheck(!restoring ? code : undefined)
  const isDrawing = invitation.state === 'valid' && invitation.kind === 'drawing'

  // Sets state only when the answer comes, so the circle-link effect below may call it.
  const requestJoin = () =>
    client
      .post<{ groupId: string }>(`/invitations/${code}/join`)
      .then(({ data }) => {
        setStatus('done')
        setTimeout(() => navigate(isDrawing ? `/drawings/${data.groupId}` : '/circles'), isDrawing ? 800 : 2000)
      })
      .catch((err: unknown) => {
        setError(inviteErrorMessage(err, t('join.failed')))
        setStatus('error')
      })

  const join = () => {
    setStatus('joining')
    requestJoin()
  }

  const decline = () => {
    client
      .post(`/invitations/${code}/decline`)
      .then(() => setStatus('declined'))
      .catch((err: unknown) => {
        setError(inviteErrorMessage(err, t('santa.actionFailed')))
        setStatus('error')
      })
  }

  // Signed in with a circle's link: join without asking, as before.
  useEffect(() => {
    if (user && invitation.state === 'valid' && invitation.kind === 'circle' && status === 'idle') requestJoin()
    // eslint-disable-next-line react-hooks/exhaustive-deps -- joining must not run again when the language changes
  }, [user, invitation.state])

  if (restoring || invitation.state === 'checking') return <WakingUp fullScreen={false} />
  if (invitation.state === 'invalid' && status === 'idle') return <InvalidInvitation message={invitation.message} />

  if (status === 'declined') {
    return (
      <Card>
        <CardHeader>
          <CardTitle>{t('join.drawing.declined')}</CardTitle>
        </CardHeader>
        <CardFooter className="justify-center">
          <Button asChild variant="outline">
            <Link to="/">{t('join.goHome')}</Link>
          </Button>
        </CardFooter>
      </Card>
    )
  }

  if (status === 'error') {
    return (
      <Card>
        <CardHeader>
          <CardTitle>{t('join.error.title')}</CardTitle>
        </CardHeader>
        <CardContent>
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        </CardContent>
        <CardFooter className="justify-center">
          <Button asChild variant="outline">
            <Link to="/">{t('join.goHome')}</Link>
          </Button>
        </CardFooter>
      </Card>
    )
  }

  if (status === 'joining' || status === 'done') {
    return (
      <Card>
        <CardHeader>
          <CardTitle>{status === 'done' ? t('join.done') : t('join.joining')}</CardTitle>
          <CardDescription>{status === 'done' ? t('join.redirecting') : t('join.moment')}</CardDescription>
        </CardHeader>
      </Card>
    )
  }

  const valid = invitation as ValidInvitation
  if (valid.kind === 'drawing') {
    return (
      <Card>
        <CardHeader>
          <CardTitle>
            <Trans k="join.drawing.invited" values={{ inviter: valid.inviterName, circle: valid.circleName }} />
          </CardTitle>
          <CardDescription>
            {user ? t('join.drawing.signedInText') : valid.signInRequired ? t('join.drawing.signInText') : t('join.drawing.text')}
          </CardDescription>
        </CardHeader>
        <CardFooter className="flex-col gap-2">
          {user ? (
            <Button className="w-full" onClick={join}>
              {t('santa.join')}
            </Button>
          ) : (
            <>
              {!valid.signInRequired && (
                <Button asChild className="w-full">
                  <Link to={`/register?invite=${code}`}>{t('register.title')}</Link>
                </Button>
              )}
              <Button asChild variant={valid.signInRequired ? 'default' : 'outline'} className="w-full">
                <Link to={`/login?redirect=/join/${code}`}>{t('login.title')}</Link>
              </Button>
            </>
          )}
          <Button variant="ghost" className="w-full" onClick={decline}>
            {t('santa.decline')}
          </Button>
        </CardFooter>
      </Card>
    )
  }

  if (user) return <WakingUp fullScreen={false} />

  return (
    <Card>
      <CardHeader>
        <CardTitle>
          <Trans k="join.invited" values={{ inviter: valid.inviterName, circle: valid.circleName }} />
        </CardTitle>
        <CardDescription>
          {t('join.invitedText')}
        </CardDescription>
      </CardHeader>
      <CardFooter className="flex-col gap-2">
        <Button asChild className="w-full">
          <Link to={`/register?invite=${code}`}>{t('register.title')}</Link>
        </Button>
        <Button asChild variant="outline" className="w-full">
          <Link to={`/login?redirect=/join/${code}`}>{t('login.title')}</Link>
        </Button>
      </CardFooter>
    </Card>
  )
}
