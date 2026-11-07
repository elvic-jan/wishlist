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

const NONE = '—'

/** Lower case without accents, so "emile" finds "Émile". */
function normalize(text: string): string {
  return text.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLocaleLowerCase()
}

function formatDate(value: string | null): string {
  if (!value) return NONE
  return new Date(value).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })
}

/** A yyyy-mm-dd day, shown as that calendar day whatever the viewer's time zone. */
function formatDay(value: string | null): string {
  if (!value) return NONE
  return new Date(`${value}T00:00:00Z`).toLocaleDateString(undefined, {
    day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC',
  })
}

function formatDateTime(value: string | null): string {
  if (!value) return NONE
  return new Date(value).toLocaleString(undefined, {
    day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
  })
}

function monthLabel(year: number, month: number): string {
  return new Date(Date.UTC(year, month - 1, 1)).toLocaleDateString(undefined, {
    month: 'short', year: 'numeric', timeZone: 'UTC',
  })
}

function Change({ current, previous, label }: { current: number; previous: number | null; label: string }) {
  if (previous === null) return <span className="text-xs text-muted-foreground">{NONE} {label}</span>
  const diff = current - previous
  return (
    <span
      className={cn(
        'text-xs',
        diff > 0 ? 'text-primary' : diff < 0 ? 'text-destructive' : 'text-muted-foreground',
      )}
    >
      {diff > 0 ? `▲ ${diff}` : diff < 0 ? `▼ ${-diff}` : 'no change'} {label}
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
  const { totals, active7, active30 } = overview
  const max = Math.max(1, ...overview.signUpsByMonth.map(m => m.count))

  return (
    <>
      <section className="space-y-3" aria-labelledby="admin-totals">
        <h2 id="admin-totals" className="text-lg font-semibold">Totals</h2>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <Tile label="Users" value={totals.users} />
          <Tile label="Circles" value={totals.circles} />
          <Tile label="Wishes" value={totals.wishes} />
          <Tile label="Active reservations" value={totals.activeReservations} />
        </div>
      </section>

      <section className="space-y-3" aria-labelledby="admin-growth">
        <h2 id="admin-growth" className="text-lg font-semibold">Growth</h2>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <Tile label="Sign-ups this month" value={overview.signUpsThisMonth}>
            <Change current={overview.signUpsThisMonth} previous={overview.signUpsLastMonth} label="vs last month" />
          </Tile>
          <Tile label="Active users, last 7 days" value={active7.current}>
            <Change current={active7.current} previous={active7.previous} label="vs 7 days before" />
          </Tile>
          <Tile label="Active users, last 30 days" value={active30.current}>
            <Change current={active30.current} previous={active30.previous} label="vs 30 days before" />
          </Tile>
        </div>

        <Card>
          <CardContent className="space-y-2 p-4">
            <p className="text-sm font-medium">Sign-ups per month</p>
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
            ? `Sign-ins and activity tracked since ${formatDay(overview.trackingSince)}.`
            : 'Sign-ins and activity haven’t been recorded yet.'}
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
  if (u.hasPassword && u.hasPasskey) return 'Password, passkey'
  if (u.hasPasskey) return 'Passkey'
  if (u.hasPassword) return 'Password'
  return NONE
}

type SortKey = 'signedUp' | 'lastActive'

function Users({ users }: { users: AdminUser[] }) {
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
      <h2 id="admin-users" className="text-lg font-semibold">Users ({users.length})</h2>
      <div className="flex flex-col gap-2 sm:flex-row">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={filter}
            onChange={e => setFilter(e.target.value)}
            placeholder="Filter by name or email"
            className="pl-9"
            aria-label="Filter by name or email"
          />
        </div>
        <div className="flex gap-1" role="group" aria-label="Sort users">
          <Button
            variant={sort === 'signedUp' ? 'secondary' : 'ghost'}
            size="sm"
            aria-pressed={sort === 'signedUp'}
            onClick={() => setSort('signedUp')}
          >
            Newest
          </Button>
          <Button
            variant={sort === 'lastActive' ? 'secondary' : 'ghost'}
            size="sm"
            aria-pressed={sort === 'lastActive'}
            onClick={() => setSort('lastActive')}
          >
            Last active
          </Button>
        </div>
      </div>

      {visible.length === 0 ? (
        <p className="py-4 text-sm text-muted-foreground">Nobody matches &ldquo;{filter}&rdquo;.</p>
      ) : (
        <ul className="space-y-2">
          {visible.map(u => (
            <li key={u.id} className="space-y-3 rounded-md border px-3 py-3 text-sm">
              <div className="flex items-start justify-between gap-2">
                <div className="min-w-0">
                  <p className="truncate font-medium">{u.displayName}</p>
                  <p className="truncate text-muted-foreground">{u.email}</p>
                </div>
                <div className="flex shrink-0 flex-wrap justify-end gap-1">
                  {u.isAdmin && <Badge variant="secondary">Admin</Badge>}
                  {!u.emailConfirmed && <Badge variant="outline">Email not confirmed</Badge>}
                </div>
              </div>
              <dl className="grid grid-cols-2 gap-x-4 gap-y-2 sm:grid-cols-3">
                <Field label="Signed up">{formatDate(u.createdAt)}</Field>
                <Field label="Last active">{formatDay(u.lastActiveOn)}</Field>
                <Field label="Last sign-in">{formatDateTime(u.lastSignInAt)}</Field>
                <Field label="Sign-ins">{u.signIns}</Field>
                <Field label="Circles">{u.circles}</Field>
                <Field label="Signs in with">
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
      .catch(() => setError('Failed to load the admin data.'))
  }, [isAdmin])

  if (!isAdmin) return <Navigate to="/wishlists" replace />

  return (
    <div className="space-y-8">
      <div className="space-y-1.5">
        <div className="flex items-center gap-2">
          <LayoutDashboard className="h-5 w-5 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">Admin</h1>
        </div>
        <p className="flex items-center gap-1.5 text-sm text-muted-foreground">
          <ShieldCheck className="h-4 w-4" aria-hidden />
          Only site admins see this page. Read-only.
        </p>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {!overview || !users ? (
        error ? null : (
          <div className="space-y-3" aria-label="Loading admin data">
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
