import { useEffect, useMemo, useState } from 'react'
import { KeyRound, LayoutDashboard, Search, ShieldCheck } from 'lucide-react'
import { Navigate } from 'react-router-dom'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { useAuth } from '@/context/AuthContext'
import { getAdminOverview, getAdminUsers, type AdminOverview, type AdminUser } from '@/api/admin'
import { cn } from '@/lib/utils'
import { getLocale, t, Trans, useT } from '@/lib/i18n'

const NONE = '—'

/** Lower case without accents, so "emile" finds "Émile". */
function normalize(text: string): string {
  return text.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLocaleLowerCase()
}

function formatDate(value: string | null): string {
  if (!value) return NONE
  return new Date(value).toLocaleDateString(getLocale(), { day: 'numeric', month: 'short', year: 'numeric' })
}

/** A yyyy-mm-dd day, shown as that calendar day whatever the viewer's time zone. */
function formatDay(value: string | null): string {
  if (!value) return NONE
  return new Date(`${value}T00:00:00Z`).toLocaleDateString(getLocale(), {
    day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC',
  })
}

function formatDateTime(value: string | null): string {
  if (!value) return NONE
  return new Date(value).toLocaleString(getLocale(), {
    day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
  })
}

function monthLabel(year: number, month: number): string {
  return new Date(Date.UTC(year, month - 1, 1)).toLocaleDateString(getLocale(), {
    month: 'short', year: 'numeric', timeZone: 'UTC',
  })
}

function Change({ current, previous, label }: { current: number; previous: number | null; label: string }) {
  const t = useT()
  if (previous === null) return <span className="text-xs text-muted-foreground">{NONE} {label}</span>
  const diff = current - previous
  return (
    <span
      className={cn(
        'text-xs',
        diff > 0 ? 'text-primary' : diff < 0 ? 'text-destructive' : 'text-muted-foreground',
      )}
    >
      {diff > 0 ? `▲ ${diff}` : diff < 0 ? `▼ ${-diff}` : t('admin.noChange')} {label}
    </span>
  )
}

function Tile({ label, value, children }: { label: string; value: number; children?: React.ReactNode }) {
  return (
    <Card>
      <CardContent className="space-y-1 p-4">
        <p className="text-xs text-muted-foreground">{label}</p>
        <p className="text-2xl font-semibold tabular-nums">{value}</p>
        {children}
      </CardContent>
    </Card>
  )
}

function Overview({ overview }: { overview: AdminOverview }) {
  const t = useT()
  const { totals, active7, active30 } = overview
  const max = Math.max(1, ...overview.signUpsByMonth.map(m => m.count))

  return (
    <>
      <section className="space-y-3" aria-labelledby="admin-totals">
        <h2 id="admin-totals" className="text-lg font-semibold">{t('admin.totals')}</h2>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <Tile label={t('admin.users')} value={totals.users} />
          <Tile label={t('nav.circles')} value={totals.circles} />
          <Tile label={t('admin.wishes')} value={totals.wishes} />
          <Tile label={t('admin.activeReservations')} value={totals.activeReservations} />
        </div>
      </section>

      <section className="space-y-3" aria-labelledby="admin-growth">
        <h2 id="admin-growth" className="text-lg font-semibold">{t('admin.growth')}</h2>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <Tile label={t('admin.signUpsThisMonth')} value={overview.signUpsThisMonth}>
            <Change current={overview.signUpsThisMonth} previous={overview.signUpsLastMonth} label={t('admin.vsLastMonth')} />
          </Tile>
          <Tile label={t('admin.active7')} value={active7.current}>
            <Change current={active7.current} previous={active7.previous} label={t('admin.vs7')} />
          </Tile>
          <Tile label={t('admin.active30')} value={active30.current}>
            <Change current={active30.current} previous={active30.previous} label={t('admin.vs30')} />
          </Tile>
        </div>

        <Card>
          <CardContent className="space-y-2 p-4">
            <p className="text-sm font-medium">{t('admin.signUpsPerMonth')}</p>
            <ul className="space-y-1.5">
              {overview.signUpsByMonth.map(m => (
                <li key={`${m.year}-${m.month}`} className="flex items-center gap-3 text-sm">
                  <span className="w-20 shrink-0 text-muted-foreground">{monthLabel(m.year, m.month)}</span>
                  <span className="h-2.5 flex-1 overflow-hidden rounded-full bg-muted" aria-hidden>
                    <span
                      className="block h-full rounded-full bg-primary"
                      style={{ width: `${(m.count / max) * 100}%` }}
                    />
                  </span>
                  <span className="w-8 shrink-0 text-right tabular-nums">{m.count}</span>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>

        <p className="text-xs text-muted-foreground">
          {overview.trackingSince
            ? t('admin.trackedSince', { date: formatDay(overview.trackingSince) })
            : t('admin.notTracked')}
        </p>
      </section>
    </>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="min-w-0">
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd className="truncate tabular-nums">{children}</dd>
    </div>
  )
}

function signInOptions(u: AdminUser): string {
  if (u.hasPassword && u.hasPasskey) return t('admin.signIn.both')
  if (u.hasPasskey) return t('security.passkey.default')
  if (u.hasPassword) return t('common.password')
  return NONE
}

type SortKey = 'signedUp' | 'lastActive'

function Users({ users }: { users: AdminUser[] }) {
  const t = useT()
  const [filter, setFilter] = useState('')
  const [sort, setSort] = useState<SortKey>('signedUp')

  const term = normalize(filter.trim())
  const visible = useMemo(() => {
    const matching = term
      ? users.filter(u => normalize(u.displayName).includes(term) || normalize(u.email).includes(term))
      : users
    return [...matching].sort((a, b) =>
      sort === 'signedUp'
        ? b.createdAt.localeCompare(a.createdAt)
        // Newest first; people without activity go last.
        : (b.lastActiveOn ?? '').localeCompare(a.lastActiveOn ?? '') || b.createdAt.localeCompare(a.createdAt),
    )
  }, [users, term, sort])

  return (
    <section className="space-y-3" aria-labelledby="admin-users">
      <h2 id="admin-users" className="text-lg font-semibold">{t('admin.usersCount', { count: users.length })}</h2>
      <div className="flex flex-col gap-2 sm:flex-row">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={filter}
            onChange={e => setFilter(e.target.value)}
            placeholder={t('admin.filter')}
            className="pl-9"
            aria-label={t('admin.filter')}
          />
        </div>
        <div className="flex gap-1" role="group" aria-label={t('admin.sort')}>
          <Button
            variant={sort === 'signedUp' ? 'secondary' : 'ghost'}
            size="sm"
            aria-pressed={sort === 'signedUp'}
            onClick={() => setSort('signedUp')}
          >
            {t('admin.sort.newest')}
          </Button>
          <Button
            variant={sort === 'lastActive' ? 'secondary' : 'ghost'}
            size="sm"
            aria-pressed={sort === 'lastActive'}
            onClick={() => setSort('lastActive')}
          >
            {t('admin.lastActive')}
          </Button>
        </div>
      </div>

      {visible.length === 0 ? (
        <p className="py-4 text-sm text-muted-foreground">
          <Trans k="wishlists.noMatch" values={{ filter }} />
        </p>
      ) : (
        <ul className="space-y-2">
          {visible.map(u => (
            <li key={u.id} className="space-y-3 rounded-md border px-3 py-3 text-sm">
              <div className="flex items-start justify-between gap-2">
                <div className="min-w-0">
                  <p translate="no" className="truncate font-medium">{u.displayName}</p>
                  <p translate="no" className="truncate text-muted-foreground">{u.email}</p>
                </div>
                <div className="flex shrink-0 flex-wrap justify-end gap-1">
                  {u.isAdmin && <Badge variant="secondary">{t('menu.admin')}</Badge>}
                  {!u.emailConfirmed && <Badge variant="outline">{t('admin.emailNotConfirmed')}</Badge>}
                </div>
              </div>
              <dl className="grid grid-cols-2 gap-x-4 gap-y-2 sm:grid-cols-3">
                <Field label={t('admin.signedUp')}>{formatDate(u.createdAt)}</Field>
                <Field label={t('admin.lastActive')}>{formatDay(u.lastActiveOn)}</Field>
                <Field label={t('admin.lastSignIn')}>{formatDateTime(u.lastSignInAt)}</Field>
                <Field label={t('admin.signIns')}>{u.signIns}</Field>
                <Field label={t('nav.circles')}>{u.circles}</Field>
                <Field label={t('admin.signsInWith')}>
                  <span className="inline-flex items-center gap-1">
                    {u.hasPasskey && <KeyRound className="h-3.5 w-3.5 text-muted-foreground" aria-hidden />}
                    {signInOptions(u)}
                  </span>
                </Field>
              </dl>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

export function AdminPage() {
  const t = useT()
  const { user } = useAuth()
  const [overview, setOverview] = useState<AdminOverview | null>(null)
  const [users, setUsers] = useState<AdminUser[] | null>(null)
  const [error, setError] = useState('')
  const isAdmin = user?.isAdmin === true

  useEffect(() => {
    if (!isAdmin) return
    Promise.all([getAdminOverview(), getAdminUsers()])
      .then(([o, u]) => {
        setOverview(o)
        setUsers(u)
      })
      .catch(() => setError(t('admin.loadFailed')))
    // eslint-disable-next-line react-hooks/exhaustive-deps -- t is the same function in every language
  }, [isAdmin])

  if (!isAdmin) return <Navigate to="/wishlists" replace />

  return (
    <div className="space-y-8">
      <div className="space-y-1.5">
        <div className="flex items-center gap-2">
          <LayoutDashboard className="h-5 w-5 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">{t('menu.admin')}</h1>
        </div>
        <p className="flex items-center gap-1.5 text-sm text-muted-foreground">
          <ShieldCheck className="h-4 w-4" aria-hidden />
          {t('admin.note')}
        </p>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {!overview || !users ? (
        error ? null : (
          <div className="space-y-3" aria-label={t('admin.loading')}>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              {[0, 1, 2, 3].map(i => <Skeleton key={i} className="h-20 w-full" />)}
            </div>
            <Skeleton className="h-48 w-full" />
            <Skeleton className="h-24 w-full" />
          </div>
        )
      ) : (
        <>
          <Overview overview={overview} />
          <Users users={users} />
        </>
      )}
    </div>
  )
}
