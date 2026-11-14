import { useCallback, useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ArrowLeft, Check, Fingerprint, LogOut, Mail, Pencil, ShieldCheck, Trash2, X } from 'lucide-react'
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
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { useToast } from '@/hooks/use-toast'
import {
  addPasskey,
  apiErrorMessage,
  getSecurityOverview,
  removePasskey,
  renamePasskey,
  resendEmailConfirmation,
  signOutOtherDevices,
  type PasskeySummary,
  type SecurityOverview,
} from '@/api/passkeys'
import { useAuth } from '@/context/AuthContext'
import { formatDate, Trans, useT } from '@/lib/i18n'
import { isFirefoxAndroid, passkeyErrorMessage, passkeysSupported } from '@/lib/webauthn'

/** "Sign-in & security": passkeys, password, email confirmation and other devices. */
export function SecurityPage() {
  const t = useT()
  const { user } = useAuth()
  const { toast } = useToast()
  const [searchParams] = useSearchParams()
  const recovered = searchParams.get('recovered') === '1'
  const [overview, setOverview] = useState<SecurityOverview | null>(null)
  const [error, setError] = useState('')
  const [adding, setAdding] = useState(false)
  const [toRemove, setToRemove] = useState<PasskeySummary | null>(null)
  const [confirmSignOut, setConfirmSignOut] = useState(false)

  const load = useCallback(
    () =>
      getSecurityOverview()
        .then(setOverview)
        .catch(() => setError(t('security.loadFailed'))),
    // eslint-disable-next-line react-hooks/exhaustive-deps -- t is the same function in every language
    [],
  )

  useEffect(() => {
    let cancelled = false
    getSecurityOverview()
      .then(data => !cancelled && setOverview(data))
      .catch(() => !cancelled && setError(t('security.loadFailed')))
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- load once
  }, [])

  const handleAdd = async () => {
    setError('')
    setAdding(true)
    try {
      await addPasskey()
      await load()
      toast({ title: t('security.added'), description: t('security.addedText') })
    } catch (err) {
      const message = passkeyErrorMessage(err) ?? apiErrorMessage(err)
      if (message) setError(message)
      else if (!(err instanceof DOMException)) setError(t('security.addFailed'))
    } finally {
      setAdding(false)
    }
  }

  const handleRemove = async () => {
    if (!toRemove) return
    setError('')
    try {
      await removePasskey(toRemove.id)
      await load()
      toast({ title: t('security.removed') })
    } catch (err) {
      setError(apiErrorMessage(err) ?? t('security.removeFailed'))
    } finally {
      setToRemove(null)
    }
  }

  const handleSignOutOthers = async () => {
    setConfirmSignOut(false)
    try {
      await signOutOtherDevices()
      toast({ title: t('security.signedOutOthers'), description: t('security.signedOutOthersText') })
    } catch {
      setError(t('security.signOutFailed'))
    }
  }

  const handleResend = async () => {
    try {
      await resendEmailConfirmation()
      toast({ title: t('banner.confirmEmail.sent'), description: t('banner.confirmEmail.checkInbox', { email: user?.email ?? '' }) })
    } catch {
      setError(t('security.resendFailed'))
    }
  }

  const onlyWayIn = overview ? !overview.hasPassword && overview.passkeys.length <= 1 : true

  return (
    <div className="space-y-6">
      <div>
        <Button asChild variant="ghost" size="sm" className="-ml-2 mb-2">
          <Link to="/circles">
            <ArrowLeft className="h-4 w-4" />
            {t('common.back')}
          </Link>
        </Button>
        <div className="flex items-center gap-2">
          <ShieldCheck className="h-5 w-5 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">{t('menu.security')}</h1>
        </div>
      </div>

      {recovered && (
        <Alert className="border-primary/40">
          <AlertDescription>
            {t('security.recovered')}
          </AlertDescription>
        </Alert>
      )}
      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('security.passkeys.title')}</CardTitle>
          <CardDescription>
            {t('security.passkeys.text')}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          {!overview ? (
            <Skeleton className="h-12 w-full" />
          ) : overview.passkeys.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('security.passkeys.none')}</p>
          ) : (
            <ul className="divide-y">
              {overview.passkeys.map(p => (
                <PasskeyRow
                  key={p.id}
                  passkey={p}
                  canRemove={!onlyWayIn}
                  onRemove={() => setToRemove(p)}
                  onRenamed={load}
                />
              ))}
            </ul>
          )}
          {passkeysSupported() ? (
            <Button type="button" variant="outline" className="w-full sm:w-auto" onClick={handleAdd} disabled={adding}>
              <Fingerprint className="mr-2 h-4 w-4" aria-hidden />
              {adding ? t('common.waitingForDevice') : t('security.passkeys.add')}
            </Button>
          ) : isFirefoxAndroid() ? (
            <p className="text-sm text-muted-foreground">
              {t('security.passkeys.firefoxAndroid')}
            </p>
          ) : (
            <p className="text-sm text-muted-foreground">
              {t('security.passkeys.unsupported')}
            </p>
          )}
          {overview && onlyWayIn && overview.passkeys.length === 1 && (
            <p className="text-xs text-muted-foreground">
              {t('security.passkeys.onlyWay')}
            </p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('common.password')}</CardTitle>
        </CardHeader>
        <CardContent className="text-sm text-muted-foreground">
          {!overview ? (
            <Skeleton className="h-5 w-48" />
          ) : overview.hasPassword ? (
            <p>{t('security.password.has')}</p>
          ) : (
            <p>{t('security.password.none')}</p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('common.email')}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap items-center justify-between gap-3 text-sm">
          <div className="flex items-center gap-2">
            <Mail className="h-4 w-4 text-muted-foreground" aria-hidden />
            <span translate="no">{user?.email}</span>
            {overview &&
              (overview.emailConfirmed ? (
                <Badge variant="secondary">{t('security.email.confirmed')}</Badge>
              ) : (
                <Badge variant="outline">{t('security.email.notConfirmed')}</Badge>
              ))}
          </div>
          {overview && !overview.emailConfirmed && (
            <Button type="button" variant="outline" size="sm" onClick={handleResend}>
              {t('security.email.send')}
            </Button>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('security.others.title')}</CardTitle>
          <CardDescription>
            {t('security.others.text')}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button type="button" variant="outline" onClick={() => setConfirmSignOut(true)}>
            <LogOut className="mr-2 h-4 w-4" aria-hidden />
            {t('security.others.signOut')}
          </Button>
        </CardContent>
      </Card>

      <AlertDialog open={toRemove !== null} onOpenChange={open => !open && setToRemove(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('security.remove.title')}</AlertDialogTitle>
            <AlertDialogDescription>
              <Trans k="security.remove.text" values={{ name: toRemove?.name ?? t('security.passkey.default') }} />
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
            <AlertDialogAction onClick={handleRemove}>{t('security.remove.confirm')}</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <AlertDialog open={confirmSignOut} onOpenChange={setConfirmSignOut}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('security.signOut.title')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('security.signOut.text')}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
            <AlertDialogAction onClick={handleSignOutOthers}>{t('security.signOut.confirm')}</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

function PasskeyRow({
  passkey,
  canRemove,
  onRemove,
  onRenamed,
}: {
  passkey: PasskeySummary
  canRemove: boolean
  onRemove: () => void
  onRenamed: () => Promise<void>
}) {
  const t = useT()
  const [editing, setEditing] = useState(false)
  const [name, setName] = useState(passkey.name ?? '')
  const [saving, setSaving] = useState(false)

  const save = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!name.trim()) return
    setSaving(true)
    try {
      await renamePasskey(passkey.id, name.trim())
      await onRenamed()
      setEditing(false)
    } finally {
      setSaving(false)
    }
  }

  return (
    <li className="flex flex-wrap items-center justify-between gap-3 py-3">
      {editing ? (
        <form onSubmit={save} className="flex flex-1 items-center gap-2">
          <Input
            aria-label={t('security.passkey.name')}
            value={name}
            maxLength={100}
            autoFocus
            onChange={e => setName(e.target.value)}
            className="h-8"
          />
          <Button type="submit" size="icon" variant="ghost" aria-label={t('security.passkey.saveName')} disabled={saving}>
            <Check className="h-4 w-4" />
          </Button>
          <Button
            type="button"
            size="icon"
            variant="ghost"
            aria-label={t('security.passkey.cancelRename')}
            onClick={() => {
              setName(passkey.name ?? '')
              setEditing(false)
            }}
          >
            <X className="h-4 w-4" />
          </Button>
        </form>
      ) : (
        <>
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <Fingerprint className="h-4 w-4 shrink-0 text-primary" aria-hidden />
              <span className="truncate text-sm font-medium" translate="no">
                {passkey.name ?? t('security.passkey.default')}
              </span>
              {passkey.isBackedUp && <Badge variant="secondary">{t('security.passkey.synced')}</Badge>}
            </div>
            <p className="mt-0.5 text-xs text-muted-foreground">
              {t('security.passkey.added', { date: formatDate(new Date(passkey.createdAt)) })}
            </p>
          </div>
          <div className="flex items-center gap-1">
            <Button type="button" size="icon" variant="ghost" aria-label={t('security.passkey.rename')} onClick={() => setEditing(true)}>
              <Pencil className="h-4 w-4" />
            </Button>
            <Button
              type="button"
              size="icon"
              variant="ghost"
              aria-label={t('security.passkey.remove')}
              disabled={!canRemove}
              onClick={onRemove}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        </>
      )}
    </li>
  )
}
