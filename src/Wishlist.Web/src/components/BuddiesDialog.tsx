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
        .catch(() => toast({ title: 'Couldn’t load your buddies.', variant: 'destructive' })),
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
    run(p, () => client.post('/wishlists/me/buddies', { userId: p.userId }), `Couldn’t add ${p.displayName}.`)

  const remove = (p: Person) =>
    run(p, () => client.delete(`/wishlists/me/buddies/${p.userId}`), `Couldn’t remove ${p.displayName}.`)

  const row = (p: Person, action: 'add' | 'remove') => (
    <li key={p.userId} className="flex items-center justify-between gap-3 py-1.5">
      <span className="min-w-0 truncate text-sm">{p.displayName}</span>
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
        {action === 'add' ? 'Make buddy' : 'Remove'}
      </Button>
    </li>
  )

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Your buddies</DialogTitle>
          <DialogDescription>
            Buddies can add, change and remove wishes on your list, for example when they know what
            you&rsquo;d like. You still won&rsquo;t see what&rsquo;s been reserved.
          </DialogDescription>
        </DialogHeader>

        {buddies === null ? (
          <div className="flex justify-center py-6">
            <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
          </div>
        ) : (
          <div className="space-y-4">
            <section className="space-y-1">
              <h3 className="text-sm font-medium">Your buddies</h3>
              {buddies.length === 0 ? (
                <p className="text-sm text-muted-foreground">No buddies yet.</p>
              ) : (
                <ul>{buddies.map(p => row(p, 'remove'))}</ul>
              )}
            </section>
            <Separator />
            <section className="space-y-1">
              <h3 className="text-sm font-medium">People in your circles</h3>
              {candidates.length === 0 ? (
                <p className="text-sm text-muted-foreground">
                  {buddies.length === 0
                    ? 'Invite people to a circle first; then you can pick buddies from them.'
                    : 'Everyone in your circles is already your buddy.'}
                </p>
              ) : (
                <ul>{candidates.map(p => row(p, 'add'))}</ul>
              )}
            </section>
            <p className="text-xs text-muted-foreground">
              We&rsquo;ll email them to let them know. Removing a buddy keeps the wishes they added.
            </p>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
