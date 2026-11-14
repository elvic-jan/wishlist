import { useEffect, useState } from 'react'
import { Scale } from 'lucide-react'
import { BackButton } from '@/components/BackButton'
import { Skeleton } from '@/components/ui/skeleton'
import { useAuth } from '@/context/AuthContext'
import { Trans, useT } from '@/lib/i18n'

interface LicensedPackage {
  name: string
  version: string
  license: string
  homepage?: string
  text: string
}

/**
 * Open-source licences: every library shipped to the browser, with its
 * licence text. The list is written at build time (build/licenses.ts) as
 * /licenses.json.
 */
export function LicensesPage() {
  const t = useT()
  const { user } = useAuth()
  const [packages, setPackages] = useState<LicensedPackage[] | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    let cancelled = false
    fetch('/licenses.json', { cache: 'no-cache' })
      .then(r => (r.ok ? r.json() : Promise.reject(new Error(String(r.status)))))
      .then((data: { packages?: LicensedPackage[] }) => {
        if (!cancelled) setPackages(Array.isArray(data.packages) ? data.packages : [])
      })
      .catch(() => {
        if (!cancelled) setFailed(true)
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
          <Scale className="h-5 w-5 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">{t('menu.licences')}</h1>
        </div>
        <p className="text-sm text-muted-foreground">
          {t('licences.intro')}
        </p>
        <p className="text-sm text-muted-foreground">
          <Trans
            k="licences.icons"
            components={{
              a: <a href="https://lucide.dev" target="_blank" rel="noreferrer" className="underline underline-offset-4 hover:text-foreground" />,
            }}
          />
        </p>
      </div>

      {failed ? (
        <p className="text-sm text-muted-foreground">{t('licences.failed')}</p>
      ) : packages === null ? (
        <div className="space-y-3">
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
        </div>
      ) : (
        <ul className="divide-y rounded-md border" translate="no">
          {packages.map(pkg => (
            <li key={`${pkg.name}@${pkg.version}`}>
              <details className="group">
                <summary className="flex cursor-pointer list-none items-baseline justify-between gap-3 px-4 py-3 text-sm hover:bg-muted/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
                  <span className="min-w-0 break-words">
                    <span className="font-medium">{pkg.name}</span>{' '}
                    <span className="text-muted-foreground">{pkg.version}</span>
                  </span>
                  <span className="shrink-0 text-muted-foreground">{pkg.license}</span>
                </summary>
                <div className="space-y-2 px-4 pb-4">
                  {pkg.homepage && (
                    <a
                      href={pkg.homepage}
                      target="_blank"
                      rel="noreferrer"
                      className="block break-all text-sm underline underline-offset-4 hover:text-foreground"
                    >
                      {pkg.homepage}
                    </a>
                  )}
                  <pre className="max-h-80 overflow-auto whitespace-pre-wrap rounded bg-muted p-3 text-xs">{pkg.text}</pre>
                </div>
              </details>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
