import { useEffect, useMemo, useState } from 'react'
import { ChevronRight, Gift, Search, Users } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { badgeVariants } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { DrawingInvitationsCard } from '@/components/DrawingInvitationsCard'
import type { DrawingRecipient } from '@/api/drawings'
import { cn } from '@/lib/utils'
import client from '@/api/client'
import { Trans, useT } from '@/lib/i18n'

export interface SharedCircle {
  id: string
  name: string
}

export interface ConnectedPerson {
  userId: string
  displayName: string
  circles: SharedCircle[]
}

/** How many circle badges a person shows before "+N more". */
const MAX_BADGES = 5

/** Lower case without accents, so "emile" finds "Émile". */
function normalize(text: string): string {
  return text.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLocaleLowerCase()
}

const rowClasses =
  'flex w-full items-center justify-between gap-3 rounded-md border px-3 py-2.5 text-sm transition-colors hover:bg-accent/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring'

function CircleBadge({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <span
      title={title}
      translate="no"
      className={cn(
        badgeVariants({ variant: 'secondary' }),
        'max-w-[9rem] font-normal hover:bg-secondary sm:max-w-[12rem]',
      )}
    >
      <span className="truncate">{children}</span>
    </span>
  )
}

/** The circles shared with a person: matching circles first, at most MAX_BADGES, then "+N more". */
function CircleBadges({ circles, term }: { circles: SharedCircle[]; term: string }) {
  const t = useT()
  const ordered = term
    ? [
        ...circles.filter(c => normalize(c.name).includes(term)),
        ...circles.filter(c => !normalize(c.name).includes(term)),
      ]
    : circles
  const shown = ordered.length > MAX_BADGES ? ordered.slice(0, MAX_BADGES) : ordered
  const hidden = ordered.slice(shown.length)

  return (
    <span className="flex flex-wrap gap-1">
      {shown.map(c => (
        <CircleBadge key={c.id} title={c.name}>
          {c.name}
        </CircleBadge>
      ))}
      {hidden.length > 0 && (
        <CircleBadge title={hidden.map(c => c.name).join(', ')}>{t('wishlists.more', { count: hidden.length })}</CircleBadge>
      )}
    </span>
  )
}

export function WishlistsPage() {
  const t = useT()
  const [people, setPeople] = useState<ConnectedPerson[] | null>(null)
  const [recipients, setRecipients] = useState<DrawingRecipient[]>([])
  const [error, setError] = useState('')
  const [filter, setFilter] = useState('')

  const load = () => {
    client
      .get<ConnectedPerson[]>('/wishlists/people')
      .then(r => setPeople(r.data))
      .catch(() => setError(t('wishlists.loadFailed')))
    // The people you drew for Secret Santa; without them the page still works.
    client
      .get<DrawingRecipient[]>('/drawings/recipients/me')
      .then(r => setRecipients(r.data))
      .catch(() => setRecipients([]))
  }

  useEffect(() => {
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps -- load once
  }, [])

  // People you drew for Secret Santa are shown once, on top; each with the drawings they were drawn in.
  const drawn = useMemo(() => {
    const byPerson = new Map<string, { name: string; userId: string | null; joined: boolean; titles: SharedCircle[] }>()
    for (const r of recipients) {
      const key = r.userId ?? `${r.drawingId}:${r.name}`
      const entry = byPerson.get(key) ?? { name: r.name, userId: r.userId, joined: r.joined, titles: [] }
      entry.titles.push({ id: r.drawingId, name: r.drawingTitle })
      byPerson.set(key, entry)
    }
    return [...byPerson.values()]
  }, [recipients])
  const drawnIds = new Set(drawn.flatMap(d => (d.userId ? [d.userId] : [])))
  const others = people?.filter(p => !drawnIds.has(p.userId)) ?? null

  const term = normalize(filter.trim())
  const visible = useMemo(
    () =>
      !others || !term
        ? others
        : others.filter(
            p => normalize(p.displayName).includes(term) || p.circles.some(c => normalize(c.name).includes(term)),
          ),
    // eslint-disable-next-line react-hooks/exhaustive-deps -- others follows people and recipients
    [people, recipients, term],
  )

  return (
    <div className="space-y-6">
      <div className="space-y-1.5">
        <div className="flex items-center gap-2">
          <Gift className="h-5 w-5 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">{t('nav.wishlists')}</h1>
        </div>
        <p className="text-sm text-muted-foreground">
          {t('wishlists.intro')}
        </p>
      </div>

      <Link to="/wishlist/me" className={cn(rowClasses, 'font-medium')}>
        <span className="flex items-center gap-2">
          <Gift className="h-4 w-4 text-primary" aria-hidden />
          {t('myWishlist.title')}
        </span>
        <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
      </Link>

      <DrawingInvitationsCard onAnswered={load} />

      {drawn.length > 0 && (
        <ul className="space-y-2" aria-label={t('santa.badge')}>
          {drawn.map(d => {
            const content = (
              <>
                <span className="min-w-0 space-y-1.5">
                  <span className="flex items-center gap-2">
                    <Gift className="h-4 w-4 shrink-0 text-primary" aria-hidden />
                    <span translate="no" className="truncate font-medium">{d.name}</span>
                  </span>
                  <span className="flex flex-wrap gap-1">
                    {d.titles.map(c => (
                      <CircleBadge key={c.id} title={c.name}>{c.name}</CircleBadge>
                    ))}
                    {!d.userId && (
                      <span className="text-xs text-muted-foreground">{t('santa.notJoinedYet')}</span>
                    )}
                  </span>
                </span>
                {d.userId && <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />}
              </>
            )
            return (
              <li key={d.userId ?? d.titles[0].id + d.name}>
                {d.userId ? (
                  <Link to={`/wishlist/${d.userId}`} className={cn(rowClasses, 'border-primary/40 bg-primary/5')}>
                    {content}
                  </Link>
                ) : (
                  <div className={cn(rowClasses, 'border-dashed hover:bg-transparent')}>{content}</div>
                )}
              </li>
            )
          })}
        </ul>
      )}

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {!people ? (
        // Still loading: never show "Nobody here yet" before the list arrives.
        error ? null : (
          <div className="space-y-2" aria-label={t('wishlists.loading')}>
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-16 w-full" />
            <Skeleton className="h-16 w-full" />
          </div>
        )
      ) : others!.length === 0 ? (
        drawn.length > 0 ? null : (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center gap-3 py-12 text-center">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary">
              <Users className="h-6 w-6" />
            </div>
            <div className="space-y-1">
              <p className="text-sm font-medium">{t('wishlists.empty.title')}</p>
              <p className="text-sm text-muted-foreground">
                {t('wishlists.empty.text')}
              </p>
            </div>
            <Button asChild variant="outline" className="mt-2">
              <Link to="/circles">{t('wishlists.goToCircles')}</Link>
            </Button>
          </CardContent>
        </Card>
        )
      ) : (
        <div className="space-y-3">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={filter}
              onChange={e => setFilter(e.target.value)}
              placeholder={t('wishlists.filter')}
              className="pl-9"
              aria-label={t('wishlists.filter')}
            />
          </div>

          {visible!.length === 0 ? (
            <p className="py-4 text-sm text-muted-foreground">
              <Trans k="wishlists.noMatch" values={{ filter }} />
            </p>
          ) : (
            <ul className="space-y-2">
              {visible!.map(p => (
                <li key={p.userId}>
                  <Link to={`/wishlist/${p.userId}`} className={rowClasses}>
                    <span className="min-w-0 space-y-1.5">
                      <span translate="no" className="block truncate font-medium">{p.displayName}</span>
                      <CircleBadges circles={p.circles} term={term} />
                    </span>
                    <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  )
}
