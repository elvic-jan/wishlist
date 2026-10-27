import { useEffect, useState } from 'react'
import { Gift, Users } from 'lucide-react'
import { useSearchParams } from 'react-router-dom'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { BackButton } from '@/components/BackButton'
import { BuddiesDialog } from '@/components/BuddiesDialog'
import { WishlistEditor, type EditableItem } from '@/components/WishlistEditor'
import { useAuth } from '@/context/AuthContext'
import client from '@/api/client'

interface WishlistItem extends EditableItem {
  createdAt: string
}

export function MyWishlistPage() {
  const { user } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()
  const [items, setItems] = useState<WishlistItem[]>([])
  const [error, setError] = useState('')
  // Until the first load finishes, show a placeholder rather than "No items yet".
  const [loaded, setLoaded] = useState(false)
  // ?buddies=1 (from the what's-new message) opens the buddies dialog.
  const buddiesOpen = searchParams.get('buddies') === '1'

  const setBuddiesOpen = (open: boolean) =>
    setSearchParams(
      prev => {
        const next = new URLSearchParams(prev)
        if (open) next.set('buddies', '1')
        else next.delete('buddies')
        return next
      },
      { replace: true },
    )

  const load = () =>
    client
      .get('/wishlists/me')
      .then(r => setItems(r.data))
      .catch(() => setError('Failed to load wishlist.'))
      .finally(() => setLoaded(true))

  useEffect(() => {
    load()
  }, [])

  return (
    <div className="space-y-6">
      <div>
        <BackButton />
        <div className="flex items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <Gift className="h-5 w-5 text-primary" aria-hidden />
            <h1 className="text-2xl font-semibold tracking-tight">My wishlist</h1>
          </div>
          <Button variant="outline" size="sm" onClick={() => setBuddiesOpen(true)}>
            <Users className="h-4 w-4" />
            Buddies
          </Button>
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

      <WishlistEditor
        items={items}
        loaded={loaded}
        onChanged={load}
        itemsPath="/wishlists/me/items"
        canEdit
        addLabel="Add item"
        emptyTitle="No items yet"
        emptyText="Add the first thing you’d love to receive."
        currentUserId={user?.id}
        deleteDescription={() =>
          'This removes the item from your wishlist for everyone. It can’t be undone.'
        }
      />

      <BuddiesDialog open={buddiesOpen} onOpenChange={setBuddiesOpen} />
    </div>
  )
}
