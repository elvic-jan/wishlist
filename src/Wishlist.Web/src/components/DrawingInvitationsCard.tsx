import { useCallback, useEffect, useState } from 'react'
import { Gift, Loader2 } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useToast } from '@/hooks/use-toast'
import client from '@/api/client'
import { parseDate, type DrawingInvitation } from '@/api/drawings'
import { formatDate, plural, Trans, useT } from '@/lib/i18n'

/** Unanswered Secret Santa invitations: Join or Decline. Hidden when there are none. */
export function DrawingInvitationsCard({ onAnswered }: { onAnswered?: () => void }) {
  const t = useT()
  const { toast } = useToast()
  const [invitations, setInvitations] = useState<DrawingInvitation[]>([])
  const [busyId, setBusyId] = useState<string | null>(null)

  const load = useCallback(
    () =>
      client
        .get<DrawingInvitation[]>('/drawings/invitations/me')
        .then(r => setInvitations(r.data))
        .catch(() => setInvitations([])),
    [],
  )

  useEffect(() => {
    load()
  }, [load])

  const answer = async (invitation: DrawingInvitation, action: 'join' | 'decline') => {
    setBusyId(invitation.drawingId)
    try {
      if (action === 'join')
        await client.post(`/drawings/${invitation.drawingId}/participants/${invitation.participantId}/join`)
      else await client.post(`/drawings/${invitation.drawingId}/decline`)
      toast({ title: t(action === 'join' ? 'santa.joined' : 'santa.declined', { title: invitation.title }) })
      onAnswered?.()
    } catch {
      toast({ title: t('santa.actionFailed'), variant: 'destructive' })
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
          <Gift className="h-4 w-4 text-primary" aria-hidden />
          {plural('santa.cards.title', invitations.length)}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <ul className="divide-y">
          {invitations.map(i => (
            <li key={i.drawingId} className="flex flex-wrap items-center justify-between gap-x-3 gap-y-2 py-2.5 first:pt-0 last:pb-0">
              <Link to={`/drawings/${i.drawingId}`} className="min-w-0 text-sm hover:underline">
                <span className="block">
                  <Trans
                    k="santa.invitation.title"
                    values={{ organizer: i.organizerName || t('common.someone'), title: i.title }}
                    components={{ b: <span className="font-medium" /> }}
                  />
                </span>
                <span className="block text-xs text-muted-foreground">
                  {t('santa.ends', { date: formatDate(parseDate(i.endDate), { day: 'numeric', month: 'long', year: 'numeric' }) })}
                </span>
              </Link>
              <div className="flex shrink-0 gap-2">
                <Button size="sm" variant="ghost" disabled={busyId !== null} onClick={() => answer(i, 'decline')}>
                  {t('santa.decline')}
                </Button>
                <Button size="sm" disabled={busyId !== null} onClick={() => answer(i, 'join')}>
                  {busyId === i.drawingId && <Loader2 className="h-4 w-4 animate-spin" />}
                  {t('santa.join')}
                </Button>
              </div>
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  )
}
