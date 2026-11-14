import { useEffect, useState } from 'react'
import { Sparkles } from 'lucide-react'
import { BackButton } from '@/components/BackButton'
import { Skeleton } from '@/components/ui/skeleton'
import { useAuth } from '@/context/AuthContext'
import { formatDate as formatLocalDate, useLanguage, useT } from '@/lib/i18n'

/** A note in every language ({ en, nl }); releases written before translations existed have plain strings. */
type Note = string | Record<string, string>

interface Release {
  version: string
  date: string
  notes: Note[]
}

function formatDate(date: string): string {
  const parsed = new Date(`${date}T12:00:00Z`)
  return Number.isNaN(parsed.getTime()) ? date : formatLocalDate(parsed, { day: 'numeric', month: 'long', year: 'numeric' })
}

/**
 * What's new: every release from 1.0.0 on, newest first. The list is written
 * at deploy time (scripts/release-version.mjs) as /releases.json.
 */
export function WhatsNewPage() {
  const t = useT()
  const language = useLanguage()
  const { user } = useAuth()
  const [releases, setReleases] = useState<Release[] | null>(null)

  useEffect(() => {
    let cancelled = false
    fetch('/releases.json', { cache: 'no-cache' })
      .then(r => (r.ok ? r.json() : { releases: [] }))
      .catch(() => ({ releases: [] }))
      .then((data: { releases?: Release[] }) => {
        if (!cancelled) setReleases(Array.isArray(data.releases) ? data.releases : [])
      })
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <div className="space-y-6">
      <div className="space-y-1.5">
        <BackButton fallback={user ? '/wishlists' : '/login'} />
        <div className="flex items-center gap-2">
          <Sparkles className="h-5 w-5 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">{t('menu.whatsNew')}</h1>
        </div>
        <p className="text-sm text-muted-foreground">{t('whatsNew.intro')}</p>
      </div>

      {releases === null ? (
        <div className="space-y-4">
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-16 w-full" />
        </div>
      ) : releases.length === 0 ? (
        <p className="text-sm text-muted-foreground">{t('whatsNew.none')}</p>
      ) : (
        <ol className="space-y-6">
          {releases.map(release => (
            <li key={release.version} className="space-y-2 border-b pb-6 last:border-b-0 last:pb-0">
              <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
                <h2 className="text-base font-semibold">{t('menu.version', { version: release.version })}</h2>
                <time dateTime={release.date} className="text-sm text-muted-foreground">
                  {formatDate(release.date)}
                </time>
              </div>
              <ul className="list-disc space-y-1 pl-5 text-sm">
                {release.notes.length > 0 ? (
                  release.notes.map((note, i) => (
                    <li key={i}>{typeof note === 'string' ? note : (note[language] ?? note.en)}</li>
                  ))
                ) : (
                  <li className="text-muted-foreground">{t('whatsNew.smallFixes')}</li>
                )}
              </ul>
            </li>
          ))}
        </ol>
      )}
    </div>
  )
}
