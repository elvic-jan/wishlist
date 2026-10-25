import { useEffect, useState } from 'react'
import { Check, ChevronRight, Copy, LogOut, Users } from 'lucide-react'
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
import { useToast } from '@/hooks/use-toast'
import client from '@/api/client'
import { useAuth } from '@/context/AuthContext'

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
  const { groupId } = useParams<{ groupId: string }>()
  const { user } = useAuth()
  const navigate = useNavigate()
  const { toast } = useToast()
  const [members, setMembers] = useState<Member[] | null>(null)
  const [circles, setCircles] = useState<Circle[] | null>(null)
  const [inviteUrl, setInviteUrl] = useState('')
  const [copied, setCopied] = useState(false)
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
      .catch(() => !cancelled && setError('Failed to load circle details.'))
    return () => {
      cancelled = true
    }
  }, [groupId])

  const circleName = circles?.find(c => c.id === groupId)?.name
  const otherCircles = circles?.filter(c => c.id !== groupId) ?? []
  // You first, then everyone else in the order the API gives.
  const orderedMembers = members
    ? [...members.filter(m => m.userId === user?.id), ...members.filter(m => m.userId !== user?.id)]
    : null

  const generateInvite = async () => {
    try {
      const { data } = await client.post(`/groups/${groupId}/invitations`)
      setInviteUrl(data.inviteUrl)
      await navigator.clipboard.writeText(data.inviteUrl)
      setCopied(true)
      toast({
        title: 'Invite link copied',
        description: 'Send it to whoever you want to add.',
      })
      setTimeout(() => setCopied(false), 3000)
    } catch {
      setError('Failed to generate invitation link.')
    }
  }

  const leaveGroup = async () => {
    try {
      await client.delete(`/groups/${groupId}/members/me`)
      navigate('/')
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setError(msg ?? 'Failed to leave circle.')
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <BackButton />
        <div className="flex items-center gap-2">
          <Users className="h-5 w-5 shrink-0 text-primary" aria-hidden />
          {circleName ? (
            <h1 className="truncate text-2xl font-semibold tracking-tight">{circleName}</h1>
          ) : (
            <Skeleton className="h-8 w-48" aria-label="Loading circle" />
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
          <CardTitle className="text-base">Members</CardTitle>
        </CardHeader>
        <CardContent>
          {!orderedMembers ? (
            <div className="space-y-2" aria-label="Loading members">
              <Skeleton className="h-11 w-full" />
              <Skeleton className="h-11 w-full" />
            </div>
          ) : (
            <ul className="space-y-2">
              {orderedMembers.map(m => (
                <li key={m.userId}>
                  {m.userId === user?.id ? (
                    <RowLink to="/wishlist/me">
                      {m.displayName}{' '}
                      <span className="font-normal text-muted-foreground">(you)</span>
                    </RowLink>
                  ) : (
                    <RowLink to={`/wishlist/${m.userId}`}>{m.displayName}</RowLink>
                  )}
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Other circles</CardTitle>
        </CardHeader>
        <CardContent>
          {!circles ? (
            <Skeleton className="h-11 w-full" aria-label="Loading circles" />
          ) : otherCircles.length === 0 ? (
            <p className="text-sm text-muted-foreground">You&rsquo;re not in any other circles.</p>
          ) : (
            <ul className="space-y-2">
              {otherCircles.map(c => (
                <li key={c.id}>
                  <RowLink to={`/circles/${c.id}`}>{c.name}</RowLink>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Separator />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <Button onClick={generateInvite} variant="outline">
          {copied ? (
            <Check className="h-4 w-4" />
          ) : (
            <Copy className="h-4 w-4" />
          )}
          {copied ? 'Copied!' : 'Generate invite link'}
        </Button>
        <Button
          onClick={() => setConfirmLeave(true)}
          variant="ghost"
          className="text-destructive hover:bg-destructive/10 hover:text-destructive"
        >
          <LogOut className="h-4 w-4" />
          Leave circle
        </Button>
      </div>

      {inviteUrl && (
        <p className="break-all rounded-md border bg-muted/50 p-3 text-xs text-muted-foreground">
          {inviteUrl}
        </p>
      )}

      <AlertDialog open={confirmLeave} onOpenChange={setConfirmLeave}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Leave this circle?</AlertDialogTitle>
            <AlertDialogDescription>
              You&rsquo;ll lose access to other members&rsquo; wishlists. You
              can rejoin later if someone shares the invite link with you
              again.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={leaveGroup}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Leave circle
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
