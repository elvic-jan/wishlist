import { useEffect, useState } from 'react'
import { ChevronRight, Gift, Plus, Users } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { CircleInvitationsCard } from '@/components/CircleInvitationsCard'
import { CreateDrawingDialog } from '@/components/CreateDrawingDialog'
import { DrawingInvitationsCard } from '@/components/DrawingInvitationsCard'
import type { DrawingSummary } from '@/api/drawings'
import { cn } from '@/lib/utils'
import client from '@/api/client'
import { MAX_CIRCLE_NAME } from '@/lib/inputLimits'
import { apiErrorMessage } from '@/api/errors'
import { useT } from '@/lib/i18n'

interface Group {
  id: string
  name: string
  createdAt: string
  kind?: 'circle' | 'drawing'
  drawing?: DrawingSummary | null
}

export function HomePage() {
  const t = useT()
  const [groups, setGroups] = useState<Group[] | null>(null)
  const [newGroupName, setNewGroupName] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [showCreateDrawing, setShowCreateDrawing] = useState(false)
  const [error, setError] = useState('')

  const loadGroups = () =>
    client
      .get('/groups/me')
      .then(r => setGroups(r.data))
      .catch((err: unknown) => {
        const msg = apiErrorMessage(err)
        setError(msg ?? t('circles.loadFailed'))
      })

  useEffect(() => {
    loadGroups()
    // eslint-disable-next-line react-hooks/exhaustive-deps -- load once
  }, [])

  const createGroup = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    try {
      const { data } = await client.post('/groups', { name: newGroupName })
      setGroups(prev => [...(prev ?? []), data])
      setNewGroupName('')
      setShowCreate(false)
    } catch (err: unknown) {
      const msg = apiErrorMessage(err)
      setError(msg ?? t('circles.createFailed'))
    }
  }

  return (
    <div className="space-y-6">
      <div className="space-y-1.5">
        <div className="flex items-center gap-2">
          <Users className="h-5 w-5 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">{t('circles.title')}</h1>
        </div>
        <p className="text-sm text-muted-foreground">
          {t('circles.intro')}
        </p>
      </div>

      {/* Both ways to start something, side by side above the list. */}
      {showCreate ? (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('circles.new')}</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={createGroup} className="space-y-3">
              <div className="space-y-1.5">
                <Label htmlFor="new-group-name">{t('circles.name')}</Label>
                <Input
                  id="new-group-name"
                  value={newGroupName}
                  onChange={e => setNewGroupName(e.target.value)}
                  maxLength={MAX_CIRCLE_NAME}
                  placeholder={t('circles.namePlaceholder')}
                  required
                  autoFocus
                />
              </div>
              <div className="flex justify-end gap-2">
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => {
                    setShowCreate(false)
                    setNewGroupName('')
                  }}
                >
                  {t('common.cancel')}
                </Button>
                <Button type="submit">{t('circles.create')}</Button>
              </div>
            </form>
          </CardContent>
        </Card>
      ) : (
        <div className="flex flex-wrap gap-2">
          <Button onClick={() => setShowCreate(true)} variant="outline">
            <Plus className="h-4 w-4" />
            {t('circles.new')}
          </Button>
          <Button onClick={() => setShowCreateDrawing(true)} variant="outline">
            <Gift className="h-4 w-4" />
            {t('santa.new')}
          </Button>
        </div>
      )}

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <DrawingInvitationsCard onAnswered={loadGroups} />
      <CircleInvitationsCard onJoined={loadGroups} />

      {!groups ? (
        // Still loading: never show "No circles yet" before the circles arrive.
        error ? null : (
          <div className="space-y-2" aria-label={t('circles.loading')}>
            <Skeleton className="h-14 w-full" />
            <Skeleton className="h-14 w-full" />
          </div>
        )
      ) : groups.length === 0 ? (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center gap-3 py-12 text-center">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary">
              <Users className="h-6 w-6" />
            </div>
            <div className="space-y-1">
              <p className="text-sm font-medium">{t('circles.empty.title')}</p>
              <p className="text-sm text-muted-foreground">
                {t('circles.empty.text')}
              </p>
            </div>
          </CardContent>
        </Card>
      ) : (
        <ul className="space-y-2">
          {groups.map(g => {
            const d = g.kind === 'drawing' ? g.drawing : null
            // Ended and cancelled drawings stay listed for a while, subdued.
            const subdued = d && (d.ended || d.cancelled)
            const status = d?.cancelled
              ? t('circles.drawing.cancelled')
              : d?.ended
                ? t('circles.drawing.ended')
                : d?.myStatus === 'invited'
                  ? t('circles.drawing.invited')
                  : null
            return (
              <li key={g.id}>
                <Link to={d ? `/drawings/${g.id}` : `/circles/${g.id}`} className="group block">
                  <Card
                    className={cn(
                      'transition-colors hover:border-primary/40 hover:bg-accent/40',
                      d?.myStatus === 'invited' && !subdued && 'border-primary/40 bg-primary/5',
                      subdued && 'opacity-60',
                    )}
                  >
                    <CardHeader className="flex flex-row items-center justify-between gap-3 space-y-0 py-4">
                      <span className="flex min-w-0 items-center gap-2">
                        {d && <Gift className="h-4 w-4 shrink-0 text-primary" aria-hidden />}
                        <CardTitle translate="no" className="truncate text-base font-medium">
                          {g.name}
                        </CardTitle>
                      </span>
                      <span className="flex shrink-0 items-center gap-2">
                        {d && <Badge variant="secondary" className="font-normal">{t('santa.badge')}</Badge>}
                        {status && <Badge variant="outline" className="font-normal">{status}</Badge>}
                        <ChevronRight className="h-4 w-4 text-muted-foreground transition-colors group-hover:text-primary" />
                      </span>
                    </CardHeader>
                  </Card>
                </Link>
              </li>
            )
          })}
        </ul>
      )}

      <CreateDrawingDialog open={showCreateDrawing} onOpenChange={setShowCreateDrawing} />
    </div>
  )
}
