import { useState, type ReactNode } from 'react'
import { ExternalLink, Gift, Plus, Trash2, UserRound, X } from 'lucide-react'
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
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Skeleton } from '@/components/ui/skeleton'
import client from '@/api/client'
import { MAX_ITEM_DESCRIPTION, MAX_ITEM_TITLE, MAX_URL } from '@/lib/inputLimits'
import { apiErrorMessage } from '@/api/errors'
import { useToast } from '@/hooks/use-toast'
import { Trans, useT } from '@/lib/i18n'

export interface AddedBy {
  userId: string
  displayName: string
}

export interface EditableItem {
  id: string
  title: string
  description?: string | null
  urls: string[]
  /** The buddy who added the wish; null when the owner added it. */
  addedBy?: AddedBy | null
}

const MAX_URLS = 10

interface FormState {
  title: string
  description: string
  urls: string[]
}

const emptyForm: FormState = { title: '', description: '', urls: [] }

function isValidHttpUrl(value: string): boolean {
  const trimmed = value.trim()
  if (!trimmed) return false
  try {
    const parsed = new URL(trimmed)
    return parsed.protocol === 'http:' || parsed.protocol === 'https:'
  } catch {
    return false
  }
}

function hostFor(rawUrl: string): string {
  try {
    const parsed = new URL(rawUrl)
    return parsed.host.replace(/^www\./, '')
  } catch {
    return rawUrl
  }
}


/**
 * A wishlist's items, with add/edit/delete when `canEdit`. Used by the owner on
 * their own list and by buddies on someone else's; `itemsPath` picks the API.
 */
export function WishlistEditor<T extends EditableItem>({
  items,
  loaded,
  onChanged,
  itemsPath,
  canEdit,
  addLabel,
  emptyTitle,
  emptyText,
  currentUserId,
  renderBadge,
  renderActions,
  deleteDescription,
}: {
  items: T[]
  loaded: boolean
  onChanged: () => void
  /** e.g. /wishlists/me/items or /wishlists/{ownerId}/items */
  itemsPath: string
  canEdit: boolean
  addLabel: string
  emptyTitle: string
  emptyText: string
  /** Shows "added by you" for the viewer's own additions. */
  currentUserId?: string
  renderBadge?: (item: T) => ReactNode
  renderActions?: (item: T) => ReactNode
  deleteDescription?: (item: T) => string
}) {
  const t = useT()
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)
  // Where the new-item form is open: at the top or bottom add button.
  const [adding, setAdding] = useState<'top' | 'bottom' | null>(null)
  const { toast } = useToast()
  const [error, setError] = useState('')
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)

  const setUrl = (idx: number, value: string) =>
    setForm(f => ({ ...f, urls: f.urls.map((u, i) => (i === idx ? value : u)) }))

  const addUrlRow = () =>
    setForm(f => (f.urls.length >= MAX_URLS ? f : { ...f, urls: [...f.urls, ''] }))

  const removeUrlRow = (idx: number) =>
    setForm(f => ({ ...f, urls: f.urls.filter((_, i) => i !== idx) }))

  const cleanUrlsForSubmit = (): string[] | null => {
    const cleaned = form.urls.map(u => u.trim()).filter(u => u.length > 0)
    if (cleaned.length > MAX_URLS) {
      setError(t('editor.tooManyUrls', { max: MAX_URLS }))
      return null
    }
    for (const u of cleaned) {
      if (!isValidHttpUrl(u)) {
        setError(t('editor.badUrl'))
        return null
      }
    }
    return cleaned
  }

  const addItem = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    const urls = cleanUrlsForSubmit()
    if (urls === null) return
    try {
      await client.post(itemsPath, {
        title: form.title,
        description: form.description || null,
        urls,
      })
      setForm(emptyForm)
      setAdding(null)
      onChanged()
      // The item lands at the end of the list, maybe off screen: confirm in place.
      toast({ title: t('editor.added'), description: form.title })
    } catch (err: unknown) {
      setError(apiErrorMessage(err) ?? t('editor.addFailed'))
    }
  }

  const saveEdit = async (id: string) => {
    setError('')
    const urls = cleanUrlsForSubmit()
    if (urls === null) return
    try {
      await client.patch(`${itemsPath}/${id}`, {
        title: form.title || null,
        description: form.description || null,
        urls,
      })
      setEditingId(null)
      setForm(emptyForm)
      onChanged()
    } catch (err: unknown) {
      setError(apiErrorMessage(err) ?? t('editor.saveFailed'))
    }
  }

  const performDelete = async (id: string) => {
    setError('')
    try {
      await client.delete(`${itemsPath}/${id}`)
    } catch (err: unknown) {
      setError(apiErrorMessage(err) ?? t('editor.deleteFailed'))
    }
    setPendingDeleteId(null)
    onChanged()
  }

  const startEdit = (item: T) => {
    setAdding(null)
    setEditingId(item.id)
    setForm({
      title: item.title,
      description: item.description ?? '',
      urls: item.urls.length > 0 ? [...item.urls] : [],
    })
  }

  const startAdd = (place: 'top' | 'bottom') => {
    setEditingId(null)
    setForm(emptyForm)
    setAdding(place)
  }

  // The bottom button is only worth it once the list is long enough to scroll.
  const showBottomAdd = items.length >= 4

  const renderAddButton = (place: 'top' | 'bottom') => (
    <Button variant="outline" onClick={() => startAdd(place)}>
      <Plus className="h-4 w-4" />
      {addLabel}
    </Button>
  )

  const renderAddForm = () => (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">{t('editor.newItem')}</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={addItem} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="new-item-title">{t('editor.title')}</Label>
            <Input
              id="new-item-title"
              value={form.title}
              onChange={e => setForm(f => ({ ...f, title: e.target.value }))}
              maxLength={MAX_ITEM_TITLE}
              required
              autoFocus
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="new-item-description">{t('editor.description')}</Label>
            <Textarea
              id="new-item-description"
              value={form.description}
              onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
              maxLength={MAX_ITEM_DESCRIPTION}
              placeholder={t('editor.descriptionPlaceholder')}
              rows={2}
            />
          </div>
          {renderUrlEditor()}
          <div className="flex justify-end gap-2">
            <Button
              type="button"
              variant="ghost"
              onClick={() => {
                setAdding(null)
                setForm(emptyForm)
              }}
            >
              {t('common.cancel')}
            </Button>
            <Button type="submit">{t('editor.addItem')}</Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )

  const renderUrlEditor = () => (
    <div className="space-y-3">
      <Label>{t('editor.links')}</Label>
      <div>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={addUrlRow}
          disabled={form.urls.length >= MAX_URLS}
        >
          <Plus className="h-4 w-4" />
          {t('editor.addLink')}
          {form.urls.length > 0 && ` (${form.urls.length}/${MAX_URLS})`}
        </Button>
      </div>
      {form.urls.length > 0 && (
        <div className="space-y-2">
          {form.urls.map((u, i) => (
            <div key={i} className="flex gap-2">
              <Input
                value={u}
                onChange={e => setUrl(i, e.target.value)}
                maxLength={MAX_URL}
                placeholder="https://…"
              />
              <Button
                type="button"
                variant="ghost"
                size="icon"
                onClick={() => removeUrlRow(i)}
                aria-label={t('editor.removeUrl')}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          ))}
        </div>
      )}
    </div>
  )

  const pendingDeleteItem = items.find(i => i.id === pendingDeleteId) ?? null

  return (
    <>
      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {!loaded ? (
        <div className="space-y-3" aria-label={t('editor.loading')}>
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
        </div>
      ) : items.length === 0 && !adding ? (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center gap-3 py-12 text-center">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary">
              <Gift className="h-6 w-6" />
            </div>
            <div className="space-y-1">
              <p className="text-sm font-medium">{emptyTitle}</p>
              <p className="text-sm text-muted-foreground">{emptyText}</p>
            </div>
            {canEdit && (
              <Button onClick={() => startAdd('top')} className="mt-2">
                <Plus className="h-4 w-4" />
                {addLabel}
              </Button>
            )}
          </CardContent>
        </Card>
      ) : (
        <>
          {canEdit && (adding === 'top' ? renderAddForm() : items.length > 0 && renderAddButton('top'))}
          <ul className="space-y-3">
            {items.map(item => (
              <li key={item.id}>
                <Card>
                  {editingId === item.id ? (
                    <CardContent className="space-y-4 pt-6">
                      <div className="space-y-1.5">
                        <Label htmlFor={`title-${item.id}`}>{t('editor.title')}</Label>
                        <Input
                          id={`title-${item.id}`}
                          value={form.title}
                          onChange={e => setForm(f => ({ ...f, title: e.target.value }))}
                          maxLength={MAX_ITEM_TITLE}
                        />
                      </div>
                      <div className="space-y-1.5">
                        <Label htmlFor={`desc-${item.id}`}>{t('editor.description')}</Label>
                        <Textarea
                          id={`desc-${item.id}`}
                          value={form.description}
                          onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                          maxLength={MAX_ITEM_DESCRIPTION}
                          rows={2}
                        />
                      </div>
                      {renderUrlEditor()}
                      <div className="flex justify-end gap-2">
                        <Button
                          type="button"
                          variant="ghost"
                          onClick={() => {
                            setEditingId(null)
                            setForm(emptyForm)
                          }}
                        >
                          {t('common.cancel')}
                        </Button>
                        <Button onClick={() => saveEdit(item.id)}>{t('common.save')}</Button>
                      </div>
                    </CardContent>
                  ) : (
                    <>
                      <CardHeader className="space-y-2">
                        <div className="flex items-start justify-between gap-3">
                          <CardTitle translate="no" className="min-w-0 break-words text-base font-medium">
                            {item.title}
                          </CardTitle>
                          <div className="flex shrink-0 items-center gap-1">
                            {renderBadge?.(item)}
                            {canEdit && (
                              <>
                                <Button variant="ghost" size="sm" onClick={() => startEdit(item)}>
                                  {t('common.edit')}
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  onClick={() => setPendingDeleteId(item.id)}
                                  aria-label={t('editor.deleteItem', { title: item.title })}
                                  className="text-muted-foreground hover:text-destructive"
                                >
                                  <Trash2 className="h-4 w-4" />
                                </Button>
                              </>
                            )}
                          </div>
                        </div>
                        {item.description && (
                          <p translate="no" className="text-sm text-muted-foreground">{item.description}</p>
                        )}
                        {item.addedBy && (
                          <p className="flex items-center gap-1 text-xs text-muted-foreground">
                            <UserRound className="h-3 w-3" aria-hidden />
                            {item.addedBy.userId === currentUserId ? (
                              t('editor.addedByYou')
                            ) : (
                              <Trans k="editor.addedBy" values={{ name: item.addedBy.displayName }} />
                            )}
                          </p>
                        )}
                      </CardHeader>
                      {item.urls.length > 0 && (
                        <CardContent className={renderActions ? 'pb-3' : undefined}>
                          <div className="flex flex-wrap gap-2">
                            {item.urls.map((u, i) => (
                              <a
                                key={i}
                                href={u}
                                target="_blank"
                                rel="noopener noreferrer"
                                translate="no"
                                className="inline-flex items-center gap-1.5 rounded-md border px-2 py-1 text-xs text-foreground transition-colors hover:bg-accent"
                              >
                                {hostFor(u)}
                                <ExternalLink className="h-3 w-3 text-muted-foreground" />
                              </a>
                            ))}
                          </div>
                        </CardContent>
                      )}
                      {renderActions?.(item)}
                    </>
                  )}
                </Card>
              </li>
            ))}
          </ul>
        </>
      )}

      {canEdit && (adding === 'bottom' ? renderAddForm() : showBottomAdd && renderAddButton('bottom'))}

      <AlertDialog
        open={pendingDeleteId !== null}
        onOpenChange={open => !open && setPendingDeleteId(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              <Trans k="editor.deleteTitle" values={{ title: pendingDeleteItem?.title ?? '' }} />
            </AlertDialogTitle>
            <AlertDialogDescription>
              {pendingDeleteItem && deleteDescription
                ? deleteDescription(pendingDeleteItem)
                : t('editor.deleteText')}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => pendingDeleteId && performDelete(pendingDeleteId)}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {t('common.delete')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
