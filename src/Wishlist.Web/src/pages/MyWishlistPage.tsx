import { useEffect, useState } from 'react'
import { ExternalLink, Gift, Plus, Trash2, X } from 'lucide-react'
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
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Skeleton } from '@/components/ui/skeleton'
import { BackButton } from '@/components/BackButton'
import client from '@/api/client'

interface WishlistItem {
  id: string
  title: string
  description?: string
  urls: string[]
  createdAt: string
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

export function MyWishlistPage() {
  const [items, setItems] = useState<WishlistItem[]>([])
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [adding, setAdding] = useState(false)
  const [error, setError] = useState('')
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)
  // Until the first load finishes, show a placeholder rather than "No items yet".
  const [loaded, setLoaded] = useState(false)

  const load = () =>
    client
      .get('/wishlists/me')
      .then(r => setItems(r.data))
      .catch(() => setError('Failed to load wishlist.'))
      .finally(() => setLoaded(true))

  useEffect(() => {
    load()
  }, [])

  const setUrl = (idx: number, value: string) =>
    setForm(f => ({ ...f, urls: f.urls.map((u, i) => (i === idx ? value : u)) }))

  const addUrlRow = () =>
    setForm(f => (f.urls.length >= MAX_URLS ? f : { ...f, urls: [...f.urls, ''] }))

  const removeUrlRow = (idx: number) =>
    setForm(f => ({ ...f, urls: f.urls.filter((_, i) => i !== idx) }))

  const cleanUrlsForSubmit = (): string[] | null => {
    const cleaned = form.urls.map(u => u.trim()).filter(u => u.length > 0)
    if (cleaned.length > MAX_URLS) {
      setError(`You can attach at most ${MAX_URLS} URLs.`)
      return null
    }
    for (const u of cleaned) {
      if (!isValidHttpUrl(u)) {
        setError('Each URL must start with http:// or https://')
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
      await client.post('/wishlists/me/items', {
        title: form.title,
        description: form.description || null,
        urls,
      })
      setForm(emptyForm)
      setAdding(false)
      load()
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setError(msg ?? 'Failed to add item.')
    }
  }

  const saveEdit = async (id: string) => {
    setError('')
    const urls = cleanUrlsForSubmit()
    if (urls === null) return
    try {
      await client.patch(`/wishlists/me/items/${id}`, {
        title: form.title || null,
        description: form.description || null,
        urls,
      })
      setEditingId(null)
      setForm(emptyForm)
      load()
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setError(msg ?? 'Failed to save.')
    }
  }

  const performDelete = async (id: string) => {
    await client.delete(`/wishlists/me/items/${id}`)
    setPendingDeleteId(null)
    load()
  }

  const startEdit = (item: WishlistItem) => {
    setEditingId(item.id)
    setForm({
      title: item.title,
      description: item.description ?? '',
      urls: item.urls.length > 0 ? [...item.urls] : [],
    })
  }

  const renderUrlEditor = () => (
    <div className="space-y-3">
      <Label>Links</Label>
      <div>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={addUrlRow}
          disabled={form.urls.length >= MAX_URLS}
        >
          <Plus className="h-4 w-4" />
          Add link
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
                placeholder="https://…"
              />
              <Button
                type="button"
                variant="ghost"
                size="icon"
                onClick={() => removeUrlRow(i)}
                aria-label="Remove URL"
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
    <div className="space-y-6">
      <div>
        <BackButton />
        <div className="flex items-center gap-2">
          <Gift className="h-5 w-5 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">My wishlist</h1>
        </div>
        <p className="mt-1.5 text-sm text-muted-foreground">
          People in your circles will see this list, but you won&rsquo;t see who
          has reserved what.
        </p>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {!loaded ? (
        <div className="space-y-3" aria-label="Loading wishlist">
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
              <p className="text-sm font-medium">No items yet</p>
              <p className="text-sm text-muted-foreground">
                Add the first thing you&rsquo;d love to receive.
              </p>
            </div>
            <Button onClick={() => setAdding(true)} className="mt-2">
              <Plus className="h-4 w-4" />
              Add item
            </Button>
          </CardContent>
        </Card>
      ) : (
        <ul className="space-y-3">
          {items.map(item => (
            <li key={item.id}>
              <Card>
                {editingId === item.id ? (
                  <CardContent className="space-y-4 pt-6">
                    <div className="space-y-1.5">
                      <Label htmlFor={`title-${item.id}`}>Title</Label>
                      <Input
                        id={`title-${item.id}`}
                        value={form.title}
                        onChange={e =>
                          setForm(f => ({ ...f, title: e.target.value }))
                        }
                      />
                    </div>
                    <div className="space-y-1.5">
                      <Label htmlFor={`desc-${item.id}`}>Description</Label>
                      <Textarea
                        id={`desc-${item.id}`}
                        value={form.description}
                        onChange={e =>
                          setForm(f => ({ ...f, description: e.target.value }))
                        }
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
                        Cancel
                      </Button>
                      <Button onClick={() => saveEdit(item.id)}>Save</Button>
                    </div>
                  </CardContent>
                ) : (
                  <>
                    <CardHeader className="space-y-2">
                      <div className="flex items-start justify-between gap-3">
                        <CardTitle className="text-base font-medium">
                          {item.title}
                        </CardTitle>
                        <div className="flex shrink-0 gap-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => startEdit(item)}
                          >
                            Edit
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => setPendingDeleteId(item.id)}
                            aria-label={`Delete ${item.title}`}
                            className="text-muted-foreground hover:text-destructive"
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </div>
                      {item.description && (
                        <p className="text-sm text-muted-foreground">
                          {item.description}
                        </p>
                      )}
                    </CardHeader>
                    {item.urls.length > 0 && (
                      <CardContent>
                        <div className="flex flex-wrap gap-2">
                          {item.urls.map((u, i) => (
                            <a
                              key={i}
                              href={u}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="inline-flex items-center gap-1.5 rounded-md border px-2 py-1 text-xs text-foreground transition-colors hover:bg-accent"
                            >
                              {hostFor(u)}
                              <ExternalLink className="h-3 w-3 text-muted-foreground" />
                            </a>
                          ))}
                        </div>
                      </CardContent>
                    )}
                  </>
                )}
              </Card>
            </li>
          ))}
        </ul>
      )}

      {adding ? (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">New item</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={addItem} className="space-y-4">
              <div className="space-y-1.5">
                <Label htmlFor="new-item-title">Title</Label>
                <Input
                  id="new-item-title"
                  value={form.title}
                  onChange={e => setForm(f => ({ ...f, title: e.target.value }))}
                  required
                  autoFocus
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="new-item-description">Description</Label>
                <Textarea
                  id="new-item-description"
                  value={form.description}
                  onChange={e =>
                    setForm(f => ({ ...f, description: e.target.value }))
                  }
                  placeholder="Optional notes for whoever picks this up."
                  rows={2}
                />
              </div>
              {renderUrlEditor()}
              <div className="flex justify-end gap-2">
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => {
                    setAdding(false)
                    setForm(emptyForm)
                  }}
                >
                  Cancel
                </Button>
                <Button type="submit">Add item</Button>
              </div>
            </form>
          </CardContent>
        </Card>
      ) : (
        items.length > 0 && (
          <Button
            variant="outline"
            onClick={() => {
              setForm(emptyForm)
              setAdding(true)
            }}
          >
            <Plus className="h-4 w-4" />
            Add item
          </Button>
        )
      )}

      <AlertDialog
        open={pendingDeleteId !== null}
        onOpenChange={open => !open && setPendingDeleteId(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              Delete &ldquo;{pendingDeleteItem?.title}&rdquo;?
            </AlertDialogTitle>
            <AlertDialogDescription>
              This removes the item from your wishlist for everyone. It
              can&rsquo;t be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => pendingDeleteId && performDelete(pendingDeleteId)}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
