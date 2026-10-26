import { useCallback, useEffect, useState } from 'react'
import { Check, ExternalLink, Gift, Lock } from 'lucide-react'
import { useParams } from 'react-router-dom'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { BackButton } from '@/components/BackButton'
import { useToast } from '@/hooks/use-toast'
import client from '@/api/client'

interface GiftClaim {
  id: string
  claimedByUserId: string
  status: 'Reserved' | 'Bought'
  isClaimedByMe: boolean
  /** When your reservation ends; only sent for your own reservations. */
  expiresAt?: string | null
}

interface WishlistItem {
  id: string
  ownerId: string
  title: string
  description?: string
  urls: string[]
  claim: GiftClaim | null
}

function hostFor(rawUrl: string): string {
  try {
    const parsed = new URL(rawUrl)
    return parsed.host.replace(/^www\./, '')
  } catch {
    return rawUrl
  }
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
  const { toast } = useToast()
  const [items, setItems] = useState<WishlistItem[]>([])
  const [error, setError] = useState('')
  // Until the first load finishes, show a placeholder rather than "Nothing here yet".
  const [loaded, setLoaded] = useState(false)

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

  const handleErr = (err: unknown, fallback: string) => {
    const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
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

  return (
    <div className="space-y-6">
      <div>
        <BackButton />
        <div className="flex items-center gap-2">
          <Gift className="h-5 w-5 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">Wishlist</h1>
        </div>
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
      ) : items.length === 0 ? (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center justify-center gap-3 py-12 text-center">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary">
              <Gift className="h-6 w-6" />
            </div>
            <div className="space-y-1">
              <p className="text-sm font-medium">Nothing here yet</p>
              <p className="text-sm text-muted-foreground">
                This person hasn&rsquo;t added any items.
              </p>
            </div>
          </CardContent>
        </Card>
      ) : (
        <ul className="space-y-3">
          {items.map(item => (
            <li key={item.id}>
              <Card>
                <CardHeader className="space-y-2">
                  <div className="flex items-start justify-between gap-3">
                    <CardTitle className="text-base font-medium">
                      {item.title}
                    </CardTitle>
                    <StatusBadge claim={item.claim} />
                  </div>
                  {item.description && (
                    <p className="text-sm text-muted-foreground">
                      {item.description}
                    </p>
                  )}
                </CardHeader>
                {item.urls.length > 0 && (
                  <CardContent className="pb-3">
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
                  {item.claim?.isClaimedByMe &&
                    item.claim.status === 'Reserved' && (
                      <Button
                        size="sm"
                        onClick={() => markBought(item.id)}
                      >
                        <Check className="h-4 w-4" />
                        Mark as bought
                      </Button>
                    )}
                  {item.claim && canKeep(item.claim) && (
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => keepReservation(item.id)}
                    >
                      Keep reservation
                    </Button>
                  )}
                  {item.claim?.isClaimedByMe && (
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => releaseClaim(item.id)}
                    >
                      Release claim
                    </Button>
                  )}
                </CardContent>
              </Card>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
