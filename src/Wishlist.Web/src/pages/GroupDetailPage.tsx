import { useEffect, useState } from 'react'
import { ChevronRight, LogOut, Users } from 'lucide-react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import { BackButton } from '@/components/BackButton'
import { InviteMenu } from '@/components/InviteMenu'
import client from '@/api/client'
import { useAuth } from '@/context/AuthContext'
import { apiErrorMessage } from '@/api/errors'
import { useT, UserText } from '@/lib/i18n'

interface Member {
  userId: string
  displayName: string
  role: string
}

interface Circle {
  id: string
  name: string
}

/** A full-width row that opens something; shows just a label. */
function RowLink({ to, children }: { to: string; children: React.ReactNode }) {
  return (
    <Link
      to={to}
      className="flex w-full items-center justify-between gap-3 rounded-md border px-3 py-2.5 text-sm font-medium transition-colors hover:bg-accent/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
    >
      <span className="truncate">{children}</span>
      <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
    </Link>
  )
}

export function GroupDetailPage() {
  const t = useT()
  const { groupId } = useParams<{ groupId: string }>()
  const { user } = useAuth()
  const navigate = useNavigate()
  const [members, setMembers] = useState<Member[] | null>(null)
  const [circles, setCircles] = useState<Circle[] | null>(null)
  const [inviteUrl, setInviteUrl] = useState('')
  const [error, setError] = useState('')
  const [confirmLeave, setConfirmLeave] = useState(false)

  useEffect(() => {
    let cancelled = false
    Promise.all([client.get<Member[]>(`/groups/${groupId}/members`), client.get<Circle[]>('/groups/me')])
      .then(([membersRes, circlesRes]) => {
        if (cancelled) return
        setMembers(membersRes.data)
        setCircles(circlesRes.data)
      })
      .catch(() => !cancelled && setError(t('circle.loadFailed')))
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- t is the same function in every language
  }, [groupId])

  const circleName = circles?.find(c => c.id === groupId)?.name
  const otherCircles = circles?.filter(c => c.id !== groupId) ?? []
  // You first, then everyone else in the order the API gives.
  const orderedMembers = members
    ? [...members.filter(m => m.userId === user?.id), ...members.filter(m => m.userId !== user?.id)]
    : null


  const leaveGroup = async () => {
    try {
      await client.delete(`/groups/${groupId}/members/me`)
      navigate('/circles')
    } catch (err: unknown) {
      const msg = apiErrorMessage(err)
      setError(msg ?? t('circle.leaveFailed'))
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <BackButton fallback="/circles" />
        <div className="flex items-center gap-2">
          <Users className="h-5 w-5 shrink-0 text-primary" aria-hidden />
          {circleName ? (
            <h1 translate="no" className="truncate text-2xl font-semibold tracking-tight">{circleName}</h1>
          ) : (
            <Skeleton className="h-8 w-48" aria-label={t('circle.loading')} />
          )}
        </div>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('circle.members')}</CardTitle>
        </CardHeader>
        <CardContent>
          {!orderedMembers ? (
            <div className="space-y-2" aria-label={t('circle.loadingMembers')}>
              <Skeleton className="h-11 w-full" />
              <Skeleton className="h-11 w-full" />
            </div>
          ) : (
            <ul className="space-y-2">
              {orderedMembers.map(m => (
                <li key={m.userId}>
                  {m.userId === user?.id ? (
                    <RowLink to="/wishlist/me">
                      <UserText>{m.displayName}</UserText>{' '}
                      <span className="font-normal text-muted-foreground">{t('circle.you')}</span>
                    </RowLink>
                  ) : (
                    <RowLink to={`/wishlist/${m.userId}`}>
                      <UserText>{m.displayName}</UserText>
                    </RowLink>
                  )}
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('circle.others')}</CardTitle>
        </CardHeader>
        <CardContent>
          {!circles ? (
            <Skeleton className="h-11 w-full" aria-label={t('circles.loading')} />
          ) : otherCircles.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('circle.noOthers')}</p>
          ) : (
            <ul className="space-y-2">
              {otherCircles.map(c => (
                <li key={c.id}>
                  <RowLink to={`/circles/${c.id}`}>
                    <UserText>{c.name}</UserText>
                  </RowLink>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Separator />

      <div className="flex flex-wrap items-center justify-between gap-3">
        {circleName ? (
          <InviteMenu
            groupId={groupId!}
            circleName={circleName}
            inviterName={user?.displayName ?? t('common.someone')}
            onLinkCreated={setInviteUrl}
          />
        ) : (
          <Skeleton className="h-10 w-36" aria-label={t('common.loading')} />
        )}
        <Button
          onClick={() => setConfirmLeave(true)}
          variant="ghost"
          className="text-destructive hover:bg-destructive/10 hover:text-destructive"
        >
          <LogOut className="h-4 w-4" />
          {t('circle.leave')}
        </Button>
      </div>

      {inviteUrl && (
        <p translate="no" className="break-all rounded-md border bg-muted/50 p-3 text-xs text-muted-foreground">
          {inviteUrl}
        </p>
      )}

      <AlertDialog open={confirmLeave} onOpenChange={setConfirmLeave}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('circle.leave.title')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('circle.leave.text')}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={leaveGroup}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {t('circle.leave')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
