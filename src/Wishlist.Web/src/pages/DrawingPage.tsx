import { useCallback, useEffect, useState } from 'react'
import { ChevronRight, Gift, Loader2, LogOut, Plus, Shuffle, Undo2, X } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useParams } from 'react-router-dom'
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
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Skeleton } from '@/components/ui/skeleton'
import { BackButton } from '@/components/BackButton'
import { DrawingPeoplePicker, type PeopleSelection } from '@/components/DrawingPeoplePicker'
import { useToast } from '@/hooks/use-toast'
import client from '@/api/client'
import { apiErrorMessage } from '@/api/errors'
import { filledInvites, parseDate, type Drawing, type DrawingParticipant, type ParticipantStatus } from '@/api/drawings'
import { cn } from '@/lib/utils'
import { formatDate, plural, Trans, useT, UserText, type TextKey } from '@/lib/i18n'

const statusKey: Record<ParticipantStatus, TextKey> = {
  invited: 'santa.status.invited',
  joined: 'santa.status.joined',
  declined: 'santa.status.declined',
}

const longDate = (value: string) => formatDate(parseDate(value), { day: 'numeric', month: 'long', year: 'numeric' })

const destructive = 'text-destructive hover:bg-destructive/10 hover:text-destructive'

/** A Secret Santa drawing: participants and their status, your recipient, and the organizer's tools. */
export function DrawingPage() {
  const t = useT()
  const { drawingId } = useParams<{ drawingId: string }>()
  const { toast } = useToast()
  const [drawing, setDrawing] = useState<Drawing | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [confirm, setConfirm] = useState<'draw' | 'cancel' | 'leave' | null>(null)
  const [adding, setAdding] = useState(false)

  const load = useCallback(
    () =>
      client
        .get<Drawing>(`/drawings/${drawingId}`)
        .then(r => setDrawing(r.data))
        .catch((err: unknown) => setError(apiErrorMessage(err) ?? t('santa.loadFailed'))),
    // eslint-disable-next-line react-hooks/exhaustive-deps -- t is the same function in every language
    [drawingId],
  )

  useEffect(() => {
    load()
  }, [load])

  /** Runs an action, then reloads; errors from the API are shown as they come. */
  const act = async (action: () => Promise<unknown>, done?: string) => {
    setBusy(true)
    setError('')
    try {
      await action()
      if (done) toast({ title: done })
    } catch (err: unknown) {
      setError(apiErrorMessage(err) ?? t('santa.actionFailed'))
    } finally {
      setBusy(false)
      setConfirm(null)
      load()
    }
  }

  if (!drawing) {
    return (
      <div className="space-y-6">
        <BackButton fallback="/circles" />
        {error ? (
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        ) : (
          <div className="space-y-3" aria-label={t('common.loading')}>
            <Skeleton className="h-8 w-56" />
            <Skeleton className="h-32 w-full" />
          </div>
        )}
      </div>
    )
  }

  const base = `/drawings/${drawing.id}`
  const active = !drawing.cancelled
  const editable = drawing.isOrganizer && active && !drawing.drawn
  const included = drawing.participants.filter(p => p.status !== 'declined')
  const unanswered = drawing.participants.filter(p => p.status === 'invited')
  const me = drawing.participants.find(p => p.isMe)

  return (
    <div className={cn('space-y-6', (drawing.cancelled || drawing.ended) && 'opacity-80')}>
      <div className="space-y-1.5">
        <BackButton fallback="/circles" />
        <div className="flex items-center gap-2">
          <Gift className="h-5 w-5 shrink-0 text-primary" aria-hidden />
          <h1 translate="no" className="truncate text-2xl font-semibold tracking-tight">{drawing.title}</h1>
          <Badge variant="secondary" className="shrink-0 font-normal">{t('santa.badge')}</Badge>
        </div>
        <p className="text-sm text-muted-foreground">
          {t('santa.ends', { date: longDate(drawing.endDate) })}
          {drawing.budget && (
            <>
              {' · '}
              <Trans k="santa.budgetValue" values={{ budget: drawing.budget }} />
            </>
          )}
          {drawing.organizerName && (
            <>
              {' · '}
              <Trans k="santa.organizedBy" values={{ name: drawing.organizerName }} />
            </>
          )}
        </p>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {drawing.cancelled && (
        <Alert>
          <AlertDescription className="flex flex-wrap items-center justify-between gap-2">
            {t('santa.cancelled')}
            <Button size="sm" variant="outline" disabled={busy} onClick={() => act(() => client.post(`${base}/undo-cancel`))}>
              <Undo2 className="h-4 w-4" />
              {t('santa.undo')}
            </Button>
          </AlertDescription>
        </Alert>
      )}
      {drawing.ended && !drawing.cancelled && (
        <Alert>
          <AlertDescription>{t('santa.ended', { date: longDate(drawing.endDate) })}</AlertDescription>
        </Alert>
      )}

      {drawing.myStatus === 'invited' && active && (
        <Card className="border-primary/40 bg-primary/5">
          <CardHeader>
            <CardTitle className="text-base">
              <Trans
                k="santa.invitation.title"
                values={{ organizer: drawing.organizerName || t('common.someone'), title: drawing.title }}
                components={{ b: <span /> }}
              />
            </CardTitle>
            <CardDescription>{t('santa.invitation.text')}</CardDescription>
          </CardHeader>
          <CardContent className="flex gap-2">
            <Button disabled={busy} onClick={() => act(() => client.post(`${base}/participants/${drawing.myParticipantId}/join`),
              t('santa.joined', { title: drawing.title }))}>
              {t('santa.join')}
            </Button>
            <Button variant="ghost" disabled={busy} onClick={() => act(() => client.post(`${base}/decline`),
              t('santa.declined', { title: drawing.title }))}>
              {t('santa.decline')}
            </Button>
          </CardContent>
        </Card>
      )}

      {drawing.myRecipient && <RecipientCard drawing={drawing} />}
      {me && !drawing.drawn && active && drawing.myStatus !== 'invited' && (
        <p className="text-sm text-muted-foreground">{t('santa.notDrawn')}</p>
      )}

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0">
          <CardTitle className="text-base">{t('santa.participants')}</CardTitle>
          {editable && (
            <Button size="sm" variant="outline" onClick={() => setAdding(true)}>
              <Plus className="h-4 w-4" />
              {t('santa.add.title')}
            </Button>
          )}
        </CardHeader>
        <CardContent>
          <ul className="space-y-2">
            {drawing.participants.map(p => (
              <ParticipantRow
                key={p.id}
                participant={p}
                canRemove={editable}
                busy={busy}
                onRemove={() => act(() => client.delete(`${base}/participants/${p.id}`))}
                onJoinForThem={() => act(() => client.post(`${base}/participants/${p.id}/join`), t('santa.joinedFor', { name: p.name }))}
              />
            ))}
          </ul>
        </CardContent>
      </Card>

      {drawing.isOrganizer && active && (!drawing.drawn || drawing.exclusions.length > 0) && (
        <ExclusionsCard
          drawing={drawing}
          editable={editable}
          busy={busy}
          onSave={pairs => act(() => client.put(`${base}/exclusions`, { pairs }))}
        />
      )}

      <div className="flex flex-wrap items-center justify-between gap-3">
        {editable ? (
          <div className="space-y-1">
            <Button disabled={busy || included.length < 3} onClick={() => setConfirm('draw')}>
              <Shuffle className="h-4 w-4" />
              {t('santa.draw')}
            </Button>
            {included.length < 3 && <p className="text-xs text-muted-foreground">{t('santa.minimum')}</p>}
          </div>
        ) : (
          <span />
        )}
        <div className="flex flex-wrap gap-2">
          {me && me.status !== 'declined' && active && drawing.myStatus !== 'invited' && (
            <Button variant="ghost" className={destructive} onClick={() => setConfirm('leave')}>
              <LogOut className="h-4 w-4" />
              {t('santa.leave')}
            </Button>
          )}
          {drawing.isOrganizer && active && (
            <Button variant="ghost" className={destructive} onClick={() => setConfirm('cancel')}>
              <X className="h-4 w-4" />
              {t('santa.cancel')}
            </Button>
          )}
        </div>
      </div>

      <AlertDialog open={confirm !== null} onOpenChange={open => !open && setConfirm(null)}>
        <AlertDialogContent>
          {confirm === 'draw' && (
            <>
              <AlertDialogHeader>
                <AlertDialogTitle>{t('santa.draw.confirmTitle')}</AlertDialogTitle>
                <AlertDialogDescription>{t('santa.draw.confirmText')}</AlertDialogDescription>
              </AlertDialogHeader>
              {unanswered.length > 0 && (
                <div className="space-y-2 text-sm">
                  <p>{t('santa.draw.unanswered')}</p>
                  <ul className="list-inside list-disc text-muted-foreground">
                    {unanswered.map(p => (
                      <li key={p.id}><UserText>{p.name}</UserText></li>
                    ))}
                  </ul>
                </div>
              )}
              <AlertDialogFooter>
                <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                <AlertDialogAction
                  disabled={busy}
                  onClick={() => act(() => client.post(`${base}/draw`, { confirmUnanswered: true }), t('santa.draw.done'))}
                >
                  {t('santa.draw')}
                </AlertDialogAction>
              </AlertDialogFooter>
            </>
          )}
          {confirm === 'cancel' && (
            <>
              <AlertDialogHeader>
                <AlertDialogTitle>{t('santa.cancel.confirmTitle')}</AlertDialogTitle>
                <AlertDialogDescription>{t('santa.cancel.confirmText')}</AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>{t('santa.cancel.keep')}</AlertDialogCancel>
                <AlertDialogAction
                  className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                  onClick={() => act(() => client.post(`${base}/cancel`))}
                >
                  {t('santa.cancel')}
                </AlertDialogAction>
              </AlertDialogFooter>
            </>
          )}
          {confirm === 'leave' && (
            <>
              <AlertDialogHeader>
                <AlertDialogTitle>{t('santa.leave.confirmTitle')}</AlertDialogTitle>
                <AlertDialogDescription>
                  {t(drawing.drawn ? 'santa.leave.confirmTextDrawn' : 'santa.leave.confirmText')}
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                <AlertDialogAction
                  className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                  onClick={() => act(() => client.post(`${base}/decline`))}
                >
                  {t('santa.leave')}
                </AlertDialogAction>
              </AlertDialogFooter>
            </>
          )}
        </AlertDialogContent>
      </AlertDialog>

      <AddPeopleDialog
        open={adding}
        onOpenChange={setAdding}
        drawing={drawing}
        onAdded={count => {
          toast({ title: count > 0 ? plural('santa.add.added', count) : t('santa.add.nobody') })
          load()
        }}
      />
    </div>
  )
}

function RecipientCard({ drawing }: { drawing: Drawing }) {
  const t = useT()
  const recipient = drawing.myRecipient!
  return (
    <Card className="border-primary/40 bg-primary/5">
      <CardHeader className="pb-2">
        <CardDescription>{t('santa.recipient.title')}</CardDescription>
        <CardTitle translate="no" className="text-2xl">{recipient.name}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3 text-sm">
        <p className="text-muted-foreground">{t('santa.recipient.secret')}</p>
        {recipient.userId ? (
          <Button asChild>
            <Link to={`/wishlist/${recipient.userId}`}>
              <Gift className="h-4 w-4" />
              <Trans k="santa.recipient.open" values={{ name: recipient.name }} />
            </Link>
          </Button>
        ) : drawing.myStatus !== 'joined' ? (
          <p>{t('santa.recipient.joinFirst')}</p>
        ) : (
          <p><Trans k="santa.recipient.notJoined" values={{ name: recipient.name }} /></p>
        )}
      </CardContent>
    </Card>
  )
}

function ParticipantRow({
  participant: p,
  canRemove,
  busy,
  onRemove,
  onJoinForThem,
}: {
  participant: DrawingParticipant
  canRemove: boolean
  busy: boolean
  onRemove: () => void
  onJoinForThem: () => void
}) {
  const t = useT()
  const label = (
    <span className="min-w-0">
      <span className="flex items-center gap-2">
        <UserText className="truncate font-medium">{p.name}</UserText>
        {p.isMe && <span className="text-muted-foreground">{t('common.you')}</span>}
      </span>
      {p.email && <UserText className="block truncate text-xs text-muted-foreground">{p.email}</UserText>}
    </span>
  )
  return (
    <li className="flex items-center justify-between gap-3 rounded-md border px-3 py-2 text-sm">
      {p.userId && !p.isMe ? (
        <Link to={`/wishlist/${p.userId}`} className="flex min-w-0 flex-1 items-center gap-1 hover:underline">
          {label}
          <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
        </Link>
      ) : (
        <span className="flex min-w-0 flex-1">{label}</span>
      )}
      <span className="flex shrink-0 items-center gap-2">
        {p.canJoinForThem && (
          <Button size="sm" variant="outline" disabled={busy} onClick={onJoinForThem}>
            {t('santa.joinForThem', { name: p.name })}
          </Button>
        )}
        <Badge
          variant={p.status === 'joined' ? 'default' : 'secondary'}
          className={cn('font-normal', p.status === 'declined' && 'line-through opacity-70')}
        >
          {t(statusKey[p.status])}
        </Badge>
        {canRemove && (
          <Button size="icon" variant="ghost" disabled={busy} aria-label={t('santa.remove', { name: p.name })} onClick={onRemove}>
            <X className="h-4 w-4" />
          </Button>
        )}
      </span>
    </li>
  )
}

function ExclusionsCard({
  drawing,
  editable,
  busy,
  onSave,
}: {
  drawing: Drawing
  editable: boolean
  busy: boolean
  onSave: (pairs: { first: string; second: string }[]) => void
}) {
  const t = useT()
  const [first, setFirst] = useState('')
  const [second, setSecond] = useState('')
  const name = (id: string) => drawing.participants.find(p => p.id === id)?.name ?? '?'
  const people = drawing.participants.filter(p => p.status !== 'declined')
  const select = 'h-9 min-w-0 flex-1 rounded-md border border-input bg-background px-2 text-sm'

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">{t('santa.exclusions.title')}</CardTitle>
        <CardDescription>{t('santa.exclusions.text')}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        {drawing.exclusions.length === 0 ? (
          <p className="text-sm text-muted-foreground">{t('santa.exclusions.none')}</p>
        ) : (
          <ul className="space-y-1">
            {drawing.exclusions.map(pair => (
              <li key={`${pair.first}-${pair.second}`} className="flex items-center justify-between gap-2 text-sm">
                <Trans k="santa.exclusions.pair" values={{ first: name(pair.first), second: name(pair.second) }} />
                {editable && (
                  <Button
                    size="icon"
                    variant="ghost"
                    disabled={busy}
                    aria-label={t('santa.exclusions.remove')}
                    onClick={() => onSave(drawing.exclusions.filter(x => x !== pair))}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                )}
              </li>
            ))}
          </ul>
        )}
        {editable && (
          <div className="flex flex-wrap items-center gap-2">
            <select className={select} value={first} onChange={e => setFirst(e.target.value)} aria-label={t('santa.exclusions.pick')}>
              <option value="">{t('santa.exclusions.pick')}</option>
              {people.map(p => (
                <option key={p.id} value={p.id} translate="no">{p.name}</option>
              ))}
            </select>
            <select className={select} value={second} onChange={e => setSecond(e.target.value)} aria-label={t('santa.exclusions.pick')}>
              <option value="">{t('santa.exclusions.pick')}</option>
              {people.filter(p => p.id !== first).map(p => (
                <option key={p.id} value={p.id} translate="no">{p.name}</option>
              ))}
            </select>
            <Button
              size="sm"
              variant="outline"
              disabled={busy || !first || !second || first === second}
              onClick={() => {
                onSave([...drawing.exclusions, { first, second }])
                setFirst('')
                setSecond('')
              }}
            >
              {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
              {t('santa.exclusions.add')}
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  )
}

function AddPeopleDialog({
  open,
  onOpenChange,
  drawing,
  onAdded,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  drawing: Drawing
  onAdded: (count: number) => void
}) {
  const t = useT()
  const [people, setPeople] = useState<PeopleSelection>({ userIds: [], invites: [] })
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const alreadyIn = drawing.participants.filter(p => p.userId).map(p => p.userId!)

  const submit = async () => {
    setError('')
    setSubmitting(true)
    try {
      const { data } = await client.post<{ added: number }>(`/drawings/${drawing.id}/participants`, {
        userIds: people.userIds,
        invites: filledInvites(people.invites),
      })
      setPeople({ userIds: [], invites: [] })
      onOpenChange(false)
      onAdded(data.added)
    } catch (err: unknown) {
      setError(apiErrorMessage(err) ?? t('santa.actionFailed'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="flex max-h-[90vh] flex-col gap-4">
        <DialogHeader>
          <DialogTitle>{t('santa.add.title')}</DialogTitle>
        </DialogHeader>
        <div className="-mx-1 space-y-4 overflow-y-auto px-1">
          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}
          {open && <DrawingPeoplePicker value={people} onChange={setPeople} alreadyIn={alreadyIn} />}
        </div>
        <DialogFooter>
          <Button variant="ghost" onClick={() => onOpenChange(false)}>
            {t('common.cancel')}
          </Button>
          <Button disabled={submitting} onClick={submit}>
            {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
            {t('santa.add.submit')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
