import { useEffect, useMemo, useRef, useState } from 'react'
import { ChevronDown, Loader2, Search } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { useToast } from '@/hooks/use-toast'
import { cn } from '@/lib/utils'
import client from '@/api/client'

interface Candidate {
  userId: string
  displayName: string
  /** Already has an open invitation to this circle. */
  invited: boolean
}

interface CandidateCircle {
  groupId: string
  name: string
  members: Candidate[]
}

/** A checkbox that can show "some selected". */
function TriStateCheckbox({
  checked,
  indeterminate,
  disabled,
  onChange,
  label,
}: {
  checked: boolean
  indeterminate: boolean
  disabled?: boolean
  onChange: () => void
  label: string
}) {
  const ref = useRef<HTMLInputElement>(null)
  useEffect(() => {
    if (ref.current) ref.current.indeterminate = indeterminate
  }, [indeterminate])
  return (
    <input
      ref={ref}
      type="checkbox"
      className="h-4 w-4 shrink-0 cursor-pointer accent-primary disabled:cursor-not-allowed"
      checked={checked}
      disabled={disabled}
      onChange={onChange}
      aria-label={label}
    />
  )
}

/**
 * Invite people you know from your other circles into this one. People are
 * grouped by circle; ticking someone ticks them in every circle they're in.
 */
export function InviteFromCirclesDialog({
  groupId,
  circleName,
  open,
  onOpenChange,
}: {
  groupId: string
  circleName: string
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const { toast } = useToast()
  const [circles, setCircles] = useState<CandidateCircle[] | null>(null)
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set())
  const [filter, setFilter] = useState('')
  const [sending, setSending] = useState(false)

  useEffect(() => {
    if (!open) return
    let cancelled = false
    client
      .get<CandidateCircle[]>(`/groups/${groupId}/invite-candidates`)
      .then(r => {
        if (cancelled) return
        setCircles(r.data)
        setSelected(new Set())
        setCollapsed(new Set())
        setFilter('')
      })
      .catch(() => {
        if (!cancelled) toast({ title: 'Couldn’t load the people from your circles.', variant: 'destructive' })
      })
    return () => {
      cancelled = true
    }
  }, [open, groupId, toast])

  const term = filter.trim().toLocaleLowerCase()
  const visible = useMemo(
    () =>
      (circles ?? [])
        .map(c => ({
          ...c,
          members: term ? c.members.filter(m => m.displayName.toLocaleLowerCase().includes(term)) : c.members,
        }))
        .filter(c => c.members.length > 0),
    [circles, term],
  )

  const toggle = (userId: string) =>
    setSelected(prev => {
      const next = new Set(prev)
      if (next.has(userId)) next.delete(userId)
      else next.add(userId)
      return next
    })

  const toggleCircle = (members: Candidate[]) => {
    const selectable = members.filter(m => !m.invited).map(m => m.userId)
    const allOn = selectable.every(id => selected.has(id))
    setSelected(prev => {
      const next = new Set(prev)
      for (const id of selectable) {
        if (allOn) next.delete(id)
        else next.add(id)
      }
      return next
    })
  }

  const toggleCollapsed = (id: string) =>
    setCollapsed(prev => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })

  const invite = async () => {
    setSending(true)
    try {
      const { data } = await client.post<{ invited: number; skipped: number }>(
        `/groups/${groupId}/circle-invitations`,
        { userIds: [...selected] },
      )
      toast({
        title: data.invited === 1 ? 'Invitation sent' : `${data.invited} invitations sent`,
        description: `They’ll get an email and can join ${circleName} from My circles.`,
      })
      onOpenChange(false)
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      toast({ title: 'Couldn’t send the invitations', description: msg, variant: 'destructive' })
    } finally {
      setSending(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="flex max-h-[85vh] flex-col gap-4">
        <DialogHeader>
          <DialogTitle>Invite people from your circles</DialogTitle>
          <DialogDescription>
            They&rsquo;ll get an email and can join {circleName} from My circles.
          </DialogDescription>
        </DialogHeader>

        {circles === null ? (
          <div className="flex justify-center py-8">
            <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
          </div>
        ) : circles.length === 0 ? (
          <p className="py-4 text-sm text-muted-foreground">
            Everyone from your other circles is already in {circleName}.
          </p>
        ) : (
          <>
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={filter}
                onChange={e => setFilter(e.target.value)}
                placeholder="Filter by name"
                className="pl-9"
                aria-label="Filter by name"
              />
            </div>

            <div className="-mx-6 min-h-0 flex-1 overflow-y-auto px-6">
              {visible.length === 0 ? (
                <p className="py-4 text-sm text-muted-foreground">Nobody matches &ldquo;{filter}&rdquo;.</p>
              ) : (
                <ul className="space-y-2">
                  {visible.map(c => {
                    // While filtering, show every matching circle open.
                    const isCollapsed = !term && collapsed.has(c.groupId)
                    const selectable = c.members.filter(m => !m.invited)
                    const count = selectable.filter(m => selected.has(m.userId)).length
                    return (
                      <li key={c.groupId} className="rounded-md border">
                        <div className="flex items-center gap-3 px-3 py-2">
                          <TriStateCheckbox
                            checked={selectable.length > 0 && count === selectable.length}
                            indeterminate={count > 0 && count < selectable.length}
                            disabled={selectable.length === 0}
                            onChange={() => toggleCircle(c.members)}
                            label={`Select everyone in ${c.name}`}
                          />
                          <button
                            type="button"
                            onClick={() => toggleCollapsed(c.groupId)}
                            className="flex min-w-0 flex-1 items-center justify-between gap-2 text-left"
                            aria-expanded={!isCollapsed}
                          >
                            <span className="min-w-0 truncate text-sm font-medium">{c.name}</span>
                            <span className="flex shrink-0 items-center gap-1.5 text-xs text-muted-foreground">
                              {count > 0 ? `${count} of ${c.members.length}` : c.members.length}
                              <ChevronDown
                                className={cn('h-4 w-4 transition-transform', isCollapsed && '-rotate-90')}
                                aria-hidden
                              />
                            </span>
                          </button>
                        </div>
                        {!isCollapsed && (
                          <ul className="border-t py-1">
                            {c.members.map(m => (
                              <li key={m.userId}>
                                <label
                                  className={cn(
                                    'flex items-center gap-3 px-3 py-1.5 text-sm',
                                    m.invited ? 'cursor-not-allowed text-muted-foreground' : 'cursor-pointer hover:bg-accent/50',
                                  )}
                                >
                                  <input
                                    type="checkbox"
                                    className="h-4 w-4 shrink-0 accent-primary"
                                    checked={m.invited || selected.has(m.userId)}
                                    disabled={m.invited}
                                    onChange={() => toggle(m.userId)}
                                  />
                                  <span className="min-w-0 flex-1 truncate">{m.displayName}</span>
                                  {m.invited && <span className="shrink-0 text-xs">Invited</span>}
                                </label>
                              </li>
                            ))}
                          </ul>
                        )}
                      </li>
                    )
                  })}
                </ul>
              )}
            </div>
          </>
        )}

        <DialogFooter className="gap-2 sm:gap-0">
          <Button variant="ghost" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={invite} disabled={selected.size === 0 || sending}>
            {sending && <Loader2 className="h-4 w-4 animate-spin" />}
            {selected.size > 0 ? `Invite (${selected.size})` : 'Invite'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
