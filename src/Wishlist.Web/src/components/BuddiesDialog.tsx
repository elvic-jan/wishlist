import { useCallback, useEffect, useState } from 'react'
import { Loader2, UserMinus, UserPlus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Separator } from '@/components/ui/separator'
import { useToast } from '@/hooks/use-toast'
import client from '@/api/client'
import { useT, UserText } from '@/lib/i18n'

interface Person {
  userId: string
  displayName: string
}

/** The owner picks their buddies: people with full control over their wishlist. */
export function BuddiesDialog({
  open,
  onOpenChange,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const t = useT()
  const { toast } = useToast()
  const [buddies, setBuddies] = useState<Person[] | null>(null)
  const [candidates, setCandidates] = useState<Person[]>([])
  const [busyId, setBusyId] = useState<string | null>(null)

  const load = useCallback(
    () =>
      Promise.all([
        client.get<Person[]>('/wishlists/me/buddies'),
        client.get<Person[]>('/wishlists/me/buddy-candidates'),
      ])
        .then(([b, c]) => {
          setBuddies(b.data)
          setCandidates(c.data)
        })
        .catch(() => toast({ title: t('buddies.loadFailed'), variant: 'destructive' })),
    // eslint-disable-next-line react-hooks/exhaustive-deps -- t is the same function in every language
    [toast],
  )

  useEffect(() => {
    if (open) load()
  }, [open, load])

  const run = async (person: Person, action: () => Promise<unknown>, failure: string) => {
    setBusyId(person.userId)
    try {
      await action()
      await load()
    } catch {
      toast({ title: failure, variant: 'destructive' })
    } finally {
      setBusyId(null)
    }
  }

  const add = (p: Person) =>
    run(p, () => client.post('/wishlists/me/buddies', { userId: p.userId }), t('buddies.addFailed', { name: p.displayName }))

  const remove = (p: Person) =>
    run(p, () => client.delete(`/wishlists/me/buddies/${p.userId}`), t('buddies.removeFailed', { name: p.displayName }))

  const row = (p: Person, action: 'add' | 'remove') => (
    <li key={p.userId} className="flex items-center justify-between gap-3 py-1.5">
      <UserText className="min-w-0 truncate text-sm">{p.displayName}</UserText>
      <Button
        size="sm"
        variant={action === 'add' ? 'outline' : 'ghost'}
        disabled={busyId !== null}
        onClick={() => (action === 'add' ? add(p) : remove(p))}
        className={action === 'remove' ? 'text-muted-foreground hover:text-destructive' : undefined}
      >
        {busyId === p.userId ? (
          <Loader2 className="h-4 w-4 animate-spin" />
        ) : action === 'add' ? (
          <UserPlus className="h-4 w-4" />
        ) : (
          <UserMinus className="h-4 w-4" />
        )}
        {action === 'add' ? t('buddies.make') : t('buddies.remove')}
      </Button>
    </li>
  )

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{t('buddies.title')}</DialogTitle>
          <DialogDescription>
            {t('buddies.description')}
          </DialogDescription>
        </DialogHeader>

        {buddies === null ? (
          <div className="flex justify-center py-6">
            <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
          </div>
        ) : (
          <div className="space-y-4">
            <section className="space-y-1">
              <h3 className="text-sm font-medium">{t('buddies.title')}</h3>
              {buddies.length === 0 ? (
                <p className="text-sm text-muted-foreground">{t('buddies.none')}</p>
              ) : (
                <ul>{buddies.map(p => row(p, 'remove'))}</ul>
              )}
            </section>
            <Separator />
            <section className="space-y-1">
              <h3 className="text-sm font-medium">{t('buddies.candidates')}</h3>
              {candidates.length === 0 ? (
                <p className="text-sm text-muted-foreground">
                  {buddies.length === 0
                    ? t('buddies.inviteFirst')
                    : t('buddies.allAdded')}
                </p>
              ) : (
                <ul>{candidates.map(p => row(p, 'add'))}</ul>
              )}
            </section>
            <p className="text-xs text-muted-foreground">
              {t('buddies.footnote')}
            </p>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
