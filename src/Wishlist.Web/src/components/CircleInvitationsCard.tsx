import { useCallback, useEffect, useState } from 'react'
import { Loader2, Mail } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useToast } from '@/hooks/use-toast'
import client from '@/api/client'

interface CircleInvitation {
  id: string
  groupId: string
  circleName: string
  invitedByName: string | null
  createdAt: string
}

/** Invitations from people in your circles to join another circle: Join or Decline. Hidden when there are none. */
export function CircleInvitationsCard({ onJoined }: { onJoined: () => void }) {
  const { toast } = useToast()
  const [invitations, setInvitations] = useState<CircleInvitation[]>([])
  const [busyId, setBusyId] = useState<string | null>(null)

  const load = useCallback(
    () =>
      client
        .get<CircleInvitation[]>('/circle-invitations/me')
        .then(r => setInvitations(r.data))
        .catch(() => setInvitations([])),
    [],
  )

  useEffect(() => {
    load()
  }, [load])

  const answer = async (invitation: CircleInvitation, action: 'accept' | 'decline') => {
    setBusyId(invitation.id)
    try {
      await client.post(`/circle-invitations/${invitation.id}/${action}`)
      if (action === 'accept') {
        toast({ title: `You joined ${invitation.circleName}` })
        onJoined()
      }
    } catch {
      toast({ title: 'Something went wrong. Please try again.', variant: 'destructive' })
    } finally {
      setBusyId(null)
      load()
    }
  }

  if (invitations.length === 0) return null

  return (
    <Card className="border-primary/40 bg-primary/5">
      <CardHeader className="pb-2">
        <CardTitle className="flex items-center gap-2 text-base">
          <Mail className="h-4 w-4 text-primary" aria-hidden />
          {invitations.length === 1 ? 'Invitation' : 'Invitations'}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <ul className="divide-y">
          {invitations.map(i => (
            <li key={i.id} className="flex flex-wrap items-center justify-between gap-x-3 gap-y-2 py-2.5 first:pt-0 last:pb-0">
              <p className="min-w-0 text-sm">
                {i.invitedByName ?? 'Someone'} invited you to{' '}
                <span className="font-medium">{i.circleName}</span>
              </p>
              <div className="flex shrink-0 gap-2">
                <Button
                  size="sm"
                  variant="ghost"
                  disabled={busyId !== null}
                  onClick={() => answer(i, 'decline')}
                >
                  Decline
                </Button>
                <Button size="sm" disabled={busyId !== null} onClick={() => answer(i, 'accept')}>
                  {busyId === i.id && <Loader2 className="h-4 w-4 animate-spin" />}
                  Join
                </Button>
              </div>
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  )
}
