import { useEffect, useState } from 'react'
import { Check, Download, ExternalLink, Loader2 } from 'lucide-react'
import { Link } from 'react-router-dom'
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
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { BackButton } from '@/components/BackButton'
import client from '@/api/client'

interface Status {
  available: boolean
  imported: boolean
  userName?: string
  importedAt?: string
  itemCount?: number
}

interface OldItem {
  legacyId: number
  title: string
  description?: string | null
  urls: string[]
  changedAt?: string | null
  suggested: boolean
}

interface Preview {
  userName: string
  items: OldItem[]
}

const OLD_SITE = 'verlanglijstje.shorturl.com'

function errorMessage(err: unknown, fallback: string): string {
  return (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? fallback
}

function formatDate(iso?: string | null): string {
  return iso ? new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' }) : ''
}

function hostFor(url: string): string {
  try {
    return new URL(url).host.replace(/^www\./, '')
  } catch {
    return url
  }
}

/** Bring your wishes from the old wishlist site, once. */
export function ImportPage() {
  const [status, setStatus] = useState<Status | null>(null)
  const [userName, setUserName] = useState('')
  const [preview, setPreview] = useState<Preview | null>(null)
  const [ticked, setTicked] = useState<Set<number>>(new Set())
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [confirmEmpty, setConfirmEmpty] = useState(false)

  useEffect(() => {
    client
      .get<Status>('/legacy-import')
      .then(r => setStatus(r.data))
      .catch(() => setError('Couldn’t check the import. Please try again later.'))
  }, [])

  const lookUp = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      const { data } = await client.post<Preview>('/legacy-import/preview', { userName })
      setPreview(data)
      setTicked(new Set(data.items.filter(i => i.suggested).map(i => i.legacyId)))
    } catch (err) {
      setError(errorMessage(err, 'Couldn’t look up that username.'))
    } finally {
      setBusy(false)
    }
  }

  const toggle = (id: number) =>
    setTicked(prev => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })

  const runImport = async () => {
    if (!preview) return
    setError('')
    setBusy(true)
    try {
      const { data } = await client.post<Status>('/legacy-import', {
        userName: preview.userName,
        itemIds: [...ticked],
      })
      setStatus(data)
      setPreview(null)
    } catch (err) {
      setError(errorMessage(err, 'The import didn’t work. Please try again.'))
    } finally {
      setBusy(false)
      setConfirmEmpty(false)
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <BackButton />
        <div className="flex items-center gap-2">
          <Download className="h-5 w-5 shrink-0 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">Import from the old site</h1>
        </div>
        <p className="mt-1.5 text-sm text-muted-foreground">
          Bring your wishes over from {OLD_SITE}. You can do this once.
        </p>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {!status && !error && <Skeleton className="h-40 w-full" aria-label="Loading" />}

      {status?.imported && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Check className="h-4 w-4 text-primary" aria-hidden />
              Your wishes are imported
            </CardTitle>
            <CardDescription>
              {status.itemCount === 1 ? '1 wish' : `${status.itemCount ?? 0} wishes`} from the old account{' '}
              <span className="font-medium text-foreground">{status.userName}</span>
              {status.importedAt && <> on {formatDate(status.importedAt)}</>}.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Button asChild>
              <Link to="/wishlist/me">Open my wishlist</Link>
            </Button>
          </CardContent>
        </Card>
      )}

      {status && !status.imported && !status.available && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Not available yet</CardTitle>
            <CardDescription>The old site’s wishes haven’t been loaded yet. Please check back later.</CardDescription>
          </CardHeader>
        </Card>
      )}

      {status && !status.imported && status.available && !preview && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Your old username</CardTitle>
            <CardDescription>The name you used to sign in on {OLD_SITE}, for example JAN.VDB.</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={lookUp} className="flex flex-col gap-3 sm:flex-row">
              <div className="flex-1 space-y-1.5">
                <Label htmlFor="old-username" className="sr-only">
                  Old username
                </Label>
                <Input
                  id="old-username"
                  value={userName}
                  onChange={e => setUserName(e.target.value)}
                  autoCapitalize="characters"
                  autoComplete="off"
                  spellCheck={false}
                  required
                  autoFocus
                />
              </div>
              <Button type="submit" disabled={busy || userName.trim().length === 0}>
                {busy && <Loader2 className="h-4 w-4 animate-spin" aria-hidden />}
                Find my wishes
              </Button>
            </form>
          </CardContent>
        </Card>
      )}

      {preview && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">
              Wishes of {preview.userName}
            </CardTitle>
            <CardDescription>
              Tick the wishes you want to bring over. Recent ones are ticked already. Reservations from the old
              site don’t carry over.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {preview.items.length === 0 ? (
              <p className="text-sm text-muted-foreground">This old account has no open wishes.</p>
            ) : (
              <ul className="space-y-2">
                {preview.items.map(item => (
                  <li key={item.legacyId}>
                    <label className="flex cursor-pointer gap-3 rounded-md border p-3 transition-colors hover:bg-accent/40">
                      <input
                        type="checkbox"
                        className="mt-0.5 h-4 w-4 shrink-0 accent-primary"
                        checked={ticked.has(item.legacyId)}
                        onChange={() => toggle(item.legacyId)}
                      />
                      <span className="min-w-0 flex-1 space-y-1">
                        <span className="block text-sm font-medium">{item.title}</span>
                        {item.description && (
                          <span className="line-clamp-2 block whitespace-pre-line text-sm text-muted-foreground">
                            {item.description}
                          </span>
                        )}
                        {item.urls.length > 0 && (
                          <span className="flex flex-wrap gap-1.5">
                            {item.urls.map(u => (
                              <span
                                key={u}
                                className="inline-flex items-center gap-1 rounded border px-1.5 py-0.5 text-xs text-muted-foreground"
                              >
                                {hostFor(u)}
                                <ExternalLink className="h-3 w-3" aria-hidden />
                              </span>
                            ))}
                          </span>
                        )}
                        {item.changedAt && (
                          <span className="block text-xs text-muted-foreground">
                            Last changed {formatDate(item.changedAt)}
                          </span>
                        )}
                      </span>
                    </label>
                  </li>
                ))}
              </ul>
            )}
            <div className="flex flex-wrap items-center justify-between gap-3">
              <Button variant="ghost" onClick={() => setPreview(null)} disabled={busy}>
                Not my account
              </Button>
              <Button onClick={() => (ticked.size === 0 ? setConfirmEmpty(true) : runImport())} disabled={busy}>
                {busy && <Loader2 className="h-4 w-4 animate-spin" aria-hidden />}
                {ticked.size === 1 ? 'Import 1 wish' : `Import ${ticked.size} wishes`}
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      <AlertDialog open={confirmEmpty} onOpenChange={setConfirmEmpty}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Import nothing?</AlertDialogTitle>
            <AlertDialogDescription>
              No wishes are ticked. This links the old account {preview?.userName} to you without bringing any
              wishes over, and you can’t import again later.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Go back</AlertDialogCancel>
            <AlertDialogAction onClick={runImport}>Import nothing</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
