import { useState } from 'react'
import { Loader2 } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { Alert, AlertDescription } from '@/components/ui/alert'
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
import { Label } from '@/components/ui/label'
import { DrawingPeoplePicker, type PeopleSelection } from '@/components/DrawingPeoplePicker'
import client from '@/api/client'
import { apiErrorMessage } from '@/api/errors'
import { filledInvites, today } from '@/api/drawings'
import { MAX_CIRCLE_NAME } from '@/lib/inputLimits'
import { plural, useT } from '@/lib/i18n'

/** Any signed-in user starts a Secret Santa drawing: title, end date, optional budget and the people. */
export function CreateDrawingDialog({ open, onOpenChange }: { open: boolean; onOpenChange: (open: boolean) => void }) {
  const t = useT()
  const navigate = useNavigate()
  const [title, setTitle] = useState('')
  const [endDate, setEndDate] = useState('')
  const [budget, setBudget] = useState('')
  const [includeMe, setIncludeMe] = useState(true)
  const [people, setPeople] = useState<PeopleSelection>({ userIds: [], invites: [] })
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const invites = filledInvites(people.invites)
  const count = people.userIds.length + invites.length + (includeMe ? 1 : 0)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      const { data } = await client.post<{ id: string }>('/drawings', {
        title,
        endDate,
        budget: budget.trim() || null,
        includeMe,
        participants: { userIds: people.userIds, invites },
      })
      onOpenChange(false)
      navigate(`/drawings/${data.id}`)
    } catch (err: unknown) {
      setError(apiErrorMessage(err) ?? t('santa.createFailed'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="flex max-h-[90vh] flex-col gap-4">
        <DialogHeader>
          <DialogTitle>{t('santa.create.title')}</DialogTitle>
          <DialogDescription>{t('santa.create.description')}</DialogDescription>
        </DialogHeader>
        <form id="create-drawing" onSubmit={submit} className="-mx-1 space-y-4 overflow-y-auto px-1">
          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}
          <div className="space-y-1.5">
            <Label htmlFor="drawing-title">{t('santa.title')}</Label>
            <Input
              id="drawing-title"
              value={title}
              onChange={e => setTitle(e.target.value)}
              maxLength={MAX_CIRCLE_NAME}
              placeholder={t('santa.titlePlaceholder')}
              required
              autoFocus
            />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label htmlFor="drawing-end">{t('santa.endDate')}</Label>
              <Input
                id="drawing-end"
                type="date"
                value={endDate}
                min={today()}
                onChange={e => setEndDate(e.target.value)}
                required
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="drawing-budget">{t('santa.budget')}</Label>
              <Input
                id="drawing-budget"
                value={budget}
                onChange={e => setBudget(e.target.value)}
                maxLength={MAX_CIRCLE_NAME}
                placeholder={t('santa.budgetPlaceholder')}
              />
            </div>
          </div>
          <p className="-mt-2 text-xs text-muted-foreground">{t('santa.endDateHint')}</p>
          <label className="flex cursor-pointer items-center gap-2 text-sm">
            <input
              type="checkbox"
              className="h-4 w-4 cursor-pointer accent-primary"
              checked={includeMe}
              onChange={e => setIncludeMe(e.target.checked)}
            />
            {t('santa.includeMe')}
          </label>
          <DrawingPeoplePicker value={people} onChange={setPeople} />
        </form>
        <DialogFooter className="items-center gap-2 sm:justify-between">
          <span className="text-sm text-muted-foreground">{plural('santa.selected', count)}</span>
          <div className="flex gap-2">
            <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" form="create-drawing" disabled={submitting}>
              {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
              {submitting ? t('santa.creating') : t('santa.create')}
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
