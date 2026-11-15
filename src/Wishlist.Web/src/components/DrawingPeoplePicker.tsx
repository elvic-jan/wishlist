import { useEffect, useRef, useState } from 'react'
import { Plus, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import client from '@/api/client'
import { useAuth } from '@/context/AuthContext'
import { MAX_DISPLAY_NAME, MAX_EMAIL } from '@/lib/inputLimits'
import { useT, UserText } from '@/lib/i18n'

interface Circle {
  id: string
  name: string
  kind?: string
}

interface Member {
  userId: string
  displayName: string
}

export interface EmailRow {
  name: string
  email: string
}

export interface PeopleSelection {
  userIds: string[]
  invites: EmailRow[]
}

/** A checkbox that can show "some selected". */
function CircleCheckbox({ checked, indeterminate, onChange, label }: {
  checked: boolean
  indeterminate: boolean
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
      className="h-4 w-4 shrink-0 cursor-pointer accent-primary"
      checked={checked}
      onChange={onChange}
      aria-label={label}
    />
  )
}

/**
 * Picks the people for a Secret Santa drawing: members of the user's circles
 * (a whole circle at once, then untick some), and names with email addresses
 * for everyone else. The server merges duplicates.
 */
export function DrawingPeoplePicker({
  value,
  onChange,
  alreadyIn = [],
}: {
  value: PeopleSelection
  onChange: (value: PeopleSelection) => void
  /** Account ids already in the drawing: shown ticked and fixed. */
  alreadyIn?: string[]
}) {
  const t = useT()
  const { user } = useAuth()
  const [circles, setCircles] = useState<{ circle: Circle; members: Member[] }[] | null>(null)

  useEffect(() => {
    let cancelled = false
    client
      .get<Circle[]>('/groups/me')
      .then(async ({ data }) => {
        const permanent = data.filter(c => (c.kind ?? 'circle') === 'circle')
        const withMembers = await Promise.all(
          permanent.map(async circle => ({
            circle,
            members: (await client.get<Member[]>(`/groups/${circle.id}/members`)).data.filter(m => m.userId !== user?.id),
          })),
        )
        if (!cancelled) setCircles(withMembers.filter(c => c.members.length > 0))
      })
      .catch(() => !cancelled && setCircles([]))
    return () => {
      cancelled = true
    }
  }, [user?.id])

  const selected = new Set(value.userIds)
  const fixed = new Set(alreadyIn)
  const setSelected = (ids: Set<string>) => onChange({ ...value, userIds: [...ids] })

  const toggle = (userId: string) => {
    const next = new Set(selected)
    if (next.has(userId)) next.delete(userId)
    else next.add(userId)
    setSelected(next)
  }

  const toggleCircle = (members: Member[]) => {
    const free = members.filter(m => !fixed.has(m.userId))
    const allOn = free.every(m => selected.has(m.userId))
    const next = new Set(selected)
    free.forEach(m => (allOn ? next.delete(m.userId) : next.add(m.userId)))
    setSelected(next)
  }

  const setRow = (index: number, row: EmailRow) =>
    onChange({ ...value, invites: value.invites.map((r, i) => (i === index ? row : r)) })

  return (
    <div className="space-y-5">
      {/* Someone in no circle (yet) only invites by email. */}
      {circles?.length !== 0 && (
      <section className="space-y-2">
        <h3 className="text-sm font-medium">{t('santa.fromCircles')}</h3>
        {!circles ? (
          <Skeleton className="h-20 w-full" />
        ) : (
          circles.map(({ circle, members }) => {
            const free = members.filter(m => !fixed.has(m.userId))
            const on = free.filter(m => selected.has(m.userId)).length
            return (
              <div key={circle.id} className="rounded-md border">
                <label className="flex cursor-pointer items-center gap-2 border-b px-3 py-2 text-sm font-medium">
                  <CircleCheckbox
                    checked={free.length > 0 && on === free.length}
                    indeterminate={on > 0 && on < free.length}
                    onChange={() => toggleCircle(members)}
                    label={t('santa.selectAll')}
                  />
                  <UserText className="truncate">{circle.name}</UserText>
                </label>
                <ul className="grid gap-1 px-3 py-2 sm:grid-cols-2">
                  {members.map(m => (
                    <li key={m.userId}>
                      <label className="flex cursor-pointer items-center gap-2 text-sm">
                        <input
                          type="checkbox"
                          className="h-4 w-4 shrink-0 cursor-pointer accent-primary disabled:cursor-not-allowed"
                          checked={fixed.has(m.userId) || selected.has(m.userId)}
                          disabled={fixed.has(m.userId)}
                          onChange={() => toggle(m.userId)}
                        />
                        <UserText className="truncate">{m.displayName}</UserText>
                      </label>
                    </li>
                  ))}
                </ul>
              </div>
            )
          })
        )}
      </section>
      )}

      <section className="space-y-2">
        <div>
          <h3 className="text-sm font-medium">{t('santa.byEmail')}</h3>
          <p className="text-xs text-muted-foreground">{t('santa.byEmailHint')}</p>
        </div>
        {value.invites.map((row, index) => (
          <div key={index} className="flex items-end gap-2">
            <div className="min-w-0 flex-1 space-y-1">
              {index === 0 && <Label htmlFor={`invite-name-${index}`}>{t('santa.name')}</Label>}
              <Input
                id={`invite-name-${index}`}
                value={row.name}
                maxLength={MAX_DISPLAY_NAME}
                aria-label={t('santa.name')}
                onChange={e => setRow(index, { ...row, name: e.target.value })}
              />
            </div>
            <div className="min-w-0 flex-[1.4] space-y-1">
              {index === 0 && <Label htmlFor={`invite-email-${index}`}>{t('santa.email')}</Label>}
              <Input
                id={`invite-email-${index}`}
                type="email"
                value={row.email}
                maxLength={MAX_EMAIL}
                aria-label={t('santa.email')}
                onChange={e => setRow(index, { ...row, email: e.target.value })}
              />
            </div>
            <Button
              type="button"
              variant="ghost"
              size="icon"
              aria-label={t('santa.removeRow')}
              onClick={() => onChange({ ...value, invites: value.invites.filter((_, i) => i !== index) })}
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
        ))}
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => onChange({ ...value, invites: [...value.invites, { name: '', email: '' }] })}
        >
          <Plus className="h-4 w-4" />
          {t('santa.addRow')}
        </Button>
      </section>
    </div>
  )
}
