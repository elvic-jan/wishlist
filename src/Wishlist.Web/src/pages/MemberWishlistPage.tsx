import { useCallback, useEffect, useState } from 'react'
import { ArrowLeft, Check, ExternalLink, Gift, Lock } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { useToast } from '@/hooks/use-toast'
import client from '@/api/client'

interface GiftClaim {
  id: string
  claimedByUserId: string
  status: 'Reserved' | 'Bought'
  isClaimedByMe: boolean
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

  const load = useCallback(
    () =>
      client
        .get(`/wishlists/${userId}`)
        .then(r => setItems(r.data))
        .catch(() => setError('Failed to load wishlist.')),
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
        <Button asChild variant="ghost" size="sm" className="-ml-2 mb-2">
          <Link to="/">
            <ArrowLeft className="h-4 w-4" />
            Back
          </Link>
        </Button>
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

      {items.length === 0 ? (
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
