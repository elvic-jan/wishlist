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
import { apiErrorMessage } from '@/api/errors'
import { formatDate as formatLocalDate, plural, pluralKey, Trans, useT } from '@/lib/i18n'

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


function formatDate(iso?: string | null): string {
  return iso ? formatLocalDate(new Date(iso)) : ''
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
  const t = useT()
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
      .catch(() => setError(t('import.checkFailed')))
    // eslint-disable-next-line react-hooks/exhaustive-deps -- check once
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
      setError(apiErrorMessage(err) ?? t('import.lookupFailed'))
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
      setError(apiErrorMessage(err) ?? t('import.failed'))
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
          <h1 className="text-2xl font-semibold tracking-tight">{t('import.title')}</h1>
        </div>
        <p className="mt-1.5 text-sm text-muted-foreground">
          {t('import.intro', { site: OLD_SITE })}
        </p>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {!status && !error && <Skeleton className="h-40 w-full" aria-label={t('common.loading')} />}

      {status?.imported && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Check className="h-4 w-4 text-primary" aria-hidden />
              {t('import.done.title')}
            </CardTitle>
            <CardDescription>
              <Trans
                k={pluralKey(status.importedAt ? 'import.done.textOn' : 'import.done.text', status.itemCount ?? 0)}
                values={{ count: status.itemCount ?? 0, user: status.userName ?? '', date: formatDate(status.importedAt) }}
                components={{ b: <span className="font-medium text-foreground" /> }}
              />
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Button asChild>
              <Link to="/wishlist/me">{t('import.done.open')}</Link>
            </Button>
          </CardContent>
        </Card>
      )}

      {status && !status.imported && !status.available && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('import.unavailable.title')}</CardTitle>
            <CardDescription>{t('import.unavailable.text')}</CardDescription>
          </CardHeader>
        </Card>
      )}

      {status && !status.imported && status.available && !preview && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('import.username.title')}</CardTitle>
            <CardDescription>{t('import.username.text', { site: OLD_SITE })}</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={lookUp} className="flex flex-col gap-3 sm:flex-row">
              <div className="flex-1 space-y-1.5">
                <Label htmlFor="old-username" className="sr-only">
                  {t('import.username.label')}
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
                {t('import.find')}
              </Button>
            </form>
          </CardContent>
        </Card>
      )}

      {preview && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">
              <Trans k="import.preview.title" values={{ user: preview.userName }} />
            </CardTitle>
            <CardDescription>
              {t('import.preview.text')}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {preview.items.length === 0 ? (
              <p className="text-sm text-muted-foreground">{t('import.preview.none')}</p>
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
                        <span translate="no" className="block text-sm font-medium">{item.title}</span>
                        {item.description && (
                          <span translate="no" className="line-clamp-2 block whitespace-pre-line text-sm text-muted-foreground">
                            {item.description}
                          </span>
                        )}
                        {item.urls.length > 0 && (
                          <span className="flex flex-wrap gap-1.5">
                            {item.urls.map(u => (
                              <span
                                key={u}
                                translate="no"
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
                            {t('import.preview.changed', { date: formatDate(item.changedAt) })}
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
                {t('import.notMine')}
              </Button>
              <Button onClick={() => (ticked.size === 0 ? setConfirmEmpty(true) : runImport())} disabled={busy}>
                {busy && <Loader2 className="h-4 w-4 animate-spin" aria-hidden />}
                {plural('import.run', ticked.size)}
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      <AlertDialog open={confirmEmpty} onOpenChange={setConfirmEmpty}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('import.empty.title')}</AlertDialogTitle>
            <AlertDialogDescription>
              <Trans k="import.empty.text" values={{ user: preview?.userName ?? '' }} />
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('import.empty.back')}</AlertDialogCancel>
            <AlertDialogAction onClick={runImport}>{t('import.empty.confirm')}</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
