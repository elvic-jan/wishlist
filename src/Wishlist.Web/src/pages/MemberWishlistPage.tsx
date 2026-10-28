import { useCallback, useEffect, useState } from 'react'
import { Check, Gift, Lock, Users } from 'lucide-react'
import { useParams } from 'react-router-dom'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { CardContent } from '@/components/ui/card'
import { BackButton } from '@/components/BackButton'
import { WishlistEditor, type EditableItem } from '@/components/WishlistEditor'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/hooks/use-toast'
import client from '@/api/client'
import { apiErrorMessage } from '@/api/errors'

interface GiftClaim {
  id: string
  claimedByUserId: string
  status: 'Reserved' | 'Bought'
  isClaimedByMe: boolean
  /** When your reservation ends; only sent for your own reservations. */
  expiresAt?: string | null
}

interface WishlistItem extends EditableItem {
  ownerId: string
  claim: GiftClaim | null
}

interface Person {
  userId: string
  displayName: string
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
}

/** "Keep reservation" is offered once the reminder is due: one month or less left. */
function canKeep(claim: GiftClaim): boolean {
  if (!claim.isClaimedByMe || claim.status !== 'Reserved' || !claim.expiresAt) return false
  const reminderDue = new Date(claim.expiresAt)
  reminderDue.setMonth(reminderDue.getMonth() - 1)
  return reminderDue.getTime() <= Date.now()
}

function StatusBadge({ claim }: { claim: GiftClaim | null }) {
  if (!claim) {
    return (
      <Badge variant="outline" className="font-normal">
        Available
      </Badge>
    )
  }
  if (claim.status === 'Bought') {
    // Bought reads as "done" via the check glyph + secondary surface;
    // we deliberately avoid an extra accent color so the design stays
    // theme-token-only.
    return (
      <Badge variant="secondary" className="font-normal">
        <Check className="h-3 w-3" />
        Bought
      </Badge>
    )
  }
  if (claim.isClaimedByMe) {
    return (
      <Badge
        variant="outline"
        className="font-normal border-primary/40 bg-primary/10 text-primary"
      >
        <Lock className="h-3 w-3" />
        Reserved by you
      </Badge>
    )
  }
  return (
    <Badge variant="outline" className="font-normal text-muted-foreground">
      Reserved
    </Badge>
  )
}

export function MemberWishlistPage() {
  const { userId } = useParams<{ userId: string }>()
  const { user } = useAuth()
  const { toast } = useToast()
  const [items, setItems] = useState<WishlistItem[]>([])
  const [error, setError] = useState('')
  // Until the first load finishes, show a placeholder rather than "Nothing here yet".
  const [loaded, setLoaded] = useState(false)
  // Set when the viewer is this person's buddy: they can add, edit and delete wishes.
  const [buddyOf, setBuddyOf] = useState<Person | null>(null)
  // Everyone the viewer shares a circle with; only used for the owner's name.
  const [people, setPeople] = useState<Person[]>([])
  const name = buddyOf?.displayName ?? people.find(p => p.userId === userId)?.displayName

  const load = useCallback(
    () =>
      client
        .get(`/wishlists/${userId}`)
        .then(r => setItems(r.data))
        .catch(() => setError('Failed to load wishlist.'))
        .finally(() => setLoaded(true)),
    [userId],
  )

  useEffect(() => {
    load()
  }, [load])

  // The owner's name for the title; the items themselves don't carry it.
  useEffect(() => {
    client
      .get<Person[]>('/wishlists/people')
      .then(r => setPeople(r.data))
      .catch(() => setPeople([]))
  }, [])

  useEffect(() => {
    client
      .get<Person[]>('/wishlists/me/buddy-of')
      .then(r => setBuddyOf(r.data.find(p => p.userId === userId) ?? null))
      .catch(() => setBuddyOf(null))
  }, [userId])

  const handleErr = (err: unknown, fallback: string) => {
    const msg = apiErrorMessage(err)
    toast({
      title: fallback,
      description: msg,
      variant: 'destructive',
    })
  }

  const reserve = async (itemId: string) => {
    try {
      await client.post(`/wishlists/${userId}/items/${itemId}/claim`)
      load()
    } catch (err) {
      handleErr(err, 'Failed to reserve.')
    }
  }

  const markBought = async (itemId: string) => {
    try {
      await client.patch(`/wishlists/${userId}/items/${itemId}/claim`)
      load()
    } catch (err) {
      handleErr(err, 'Failed to update status.')
    }
  }

  const keepReservation = async (itemId: string) => {
    try {
      const { data } = await client.post<WishlistItem>(`/wishlists/${userId}/items/${itemId}/claim/extend`)
      const until = data.claim?.expiresAt
      toast({ title: 'Reservation kept', description: until ? `It now ends on ${formatDate(until)}.` : undefined })
      load()
    } catch (err) {
      handleErr(err, 'Failed to keep the reservation.')
    }
  }

  const releaseClaim = async (itemId: string) => {
    try {
      await client.delete(`/wishlists/${userId}/items/${itemId}/claim`)
      load()
    } catch (err) {
      handleErr(err, 'Failed to release claim.')
    }
  }

  const renderActions = (item: WishlistItem) => (
    <CardContent className="flex flex-wrap gap-2 pt-0">
      {item.claim?.isClaimedByMe &&
        item.claim.status === 'Reserved' &&
        item.claim.expiresAt && (
          <p className="w-full text-xs text-muted-foreground">
            Your reservation ends on {formatDate(item.claim.expiresAt)}.
          </p>
        )}
      {!item.claim && (
        <Button size="sm" onClick={() => reserve(item.id)}>
          Reserve
        </Button>
      )}
      {item.claim?.isClaimedByMe && item.claim.status === 'Reserved' && (
        <Button size="sm" onClick={() => markBought(item.id)}>
          <Check className="h-4 w-4" />
          Mark as bought
        </Button>
      )}
      {item.claim && canKeep(item.claim) && (
        <Button size="sm" variant="outline" onClick={() => keepReservation(item.id)}>
          Keep reservation
        </Button>
      )}
      {item.claim?.isClaimedByMe && (
        <Button size="sm" variant="outline" onClick={() => releaseClaim(item.id)}>
          Release claim
        </Button>
      )}
    </CardContent>
  )

  const deleteDescription = (item: WishlistItem) => {
    const name = buddyOf?.displayName ?? 'their'
    const base = `This removes the item from ${buddyOf ? `${name}’s` : 'the'} wishlist for everyone. It can’t be undone.`
    if (!item.claim || item.claim.isClaimedByMe) return base
    const what = item.claim.status === 'Bought' ? 'already bought' : 'reserved'
    return `Someone has ${what} this gift. ${base}`
  }

  return (
    <div className="space-y-6">
      <div>
        <BackButton />
        <div className="flex items-center gap-2">
          <Gift className="h-5 w-5 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">
            {name ? `${name}’s wishlist` : 'Wishlist'}
          </h1>
        </div>
        {buddyOf && (
          <p className="mt-1.5 flex items-start gap-1.5 text-sm text-muted-foreground">
            <Users className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
            <span>
              You&rsquo;re {buddyOf.displayName}&rsquo;s buddy, so you can add, change and remove
              wishes here. {buddyOf.displayName} won&rsquo;t see what&rsquo;s reserved.
            </span>
          </p>
        )}
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
        itemsPath={`/wishlists/${userId}/items`}
        canEdit={buddyOf !== null}
        addLabel={buddyOf ? `Add a wish for ${buddyOf.displayName}` : 'Add item'}
        emptyTitle="Nothing here yet"
        emptyText="This person hasn’t added any items."
        currentUserId={user?.id}
        renderBadge={item => <StatusBadge claim={item.claim} />}
        renderActions={renderActions}
        deleteDescription={deleteDescription}
      />
    </div>
  )
}
