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
import { formatDate as formatLocalDate, Trans, useT } from '@/lib/i18n'

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
  return formatLocalDate(new Date(iso))
}

/** "Keep reservation" is offered once the reminder is due: one month or less left. */
function canKeep(claim: GiftClaim): boolean {
  if (!claim.isClaimedByMe || claim.status !== 'Reserved' || !claim.expiresAt) return false
  const reminderDue = new Date(claim.expiresAt)
  reminderDue.setMonth(reminderDue.getMonth() - 1)
  return reminderDue.getTime() <= Date.now()
}

function StatusBadge({ claim }: { claim: GiftClaim | null }) {
  const t = useT()
  if (!claim) {
    return (
      <Badge variant="outline" className="font-normal">
        {t('claim.available')}
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
        {t('claim.bought')}
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
        {t('claim.reservedByYou')}
      </Badge>
    )
  }
  return (
    <Badge variant="outline" className="font-normal text-muted-foreground">
      {t('claim.reserved')}
    </Badge>
  )
}

export function MemberWishlistPage() {
  const t = useT()
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
        .catch(() => setError(t('myWishlist.loadFailed')))
        .finally(() => setLoaded(true)),
    // eslint-disable-next-line react-hooks/exhaustive-deps -- t is the same function in every language
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
      handleErr(err, t('member.reserveFailed'))
    }
  }

  const markBought = async (itemId: string) => {
    try {
      await client.patch(`/wishlists/${userId}/items/${itemId}/claim`)
      load()
    } catch (err) {
      handleErr(err, t('member.statusFailed'))
    }
  }

  const keepReservation = async (itemId: string) => {
    try {
      const { data } = await client.post<WishlistItem>(`/wishlists/${userId}/items/${itemId}/claim/extend`)
      const until = data.claim?.expiresAt
      toast({ title: t('member.kept'), description: until ? t('member.keptUntil', { date: formatDate(until) }) : undefined })
      load()
    } catch (err) {
      handleErr(err, t('member.keepFailed'))
    }
  }

  const releaseClaim = async (itemId: string) => {
    try {
      await client.delete(`/wishlists/${userId}/items/${itemId}/claim`)
      load()
    } catch (err) {
      handleErr(err, t('member.releaseFailed'))
    }
  }

  const renderActions = (item: WishlistItem) => (
    <CardContent className="flex flex-wrap gap-2 pt-0">
      {item.claim?.isClaimedByMe &&
        item.claim.status === 'Reserved' &&
        item.claim.expiresAt && (
          <p className="w-full text-xs text-muted-foreground">
            {t('member.endsOn', { date: formatDate(item.claim.expiresAt) })}
          </p>
        )}
      {!item.claim && (
        <Button size="sm" onClick={() => reserve(item.id)}>
          {t('member.reserve')}
        </Button>
      )}
      {item.claim?.isClaimedByMe && item.claim.status === 'Reserved' && (
        <Button size="sm" onClick={() => markBought(item.id)}>
          <Check className="h-4 w-4" />
          {t('member.markBought')}
        </Button>
      )}
      {item.claim && canKeep(item.claim) && (
        <Button size="sm" variant="outline" onClick={() => keepReservation(item.id)}>
          {t('member.keep')}
        </Button>
      )}
      {item.claim?.isClaimedByMe && (
        <Button size="sm" variant="outline" onClick={() => releaseClaim(item.id)}>
          {t('member.release')}
        </Button>
      )}
    </CardContent>
  )

  const deleteDescription = (item: WishlistItem) => {
    const text = buddyOf ? t('member.deleteNamed', { name: buddyOf.displayName }) : t('editor.deleteText')
    if (!item.claim || item.claim.isClaimedByMe) return text
    return t(item.claim.status === 'Bought' ? 'member.deleteBought' : 'member.deleteReserved', { text })
  }

  return (
    <div className="space-y-6">
      <div>
        <BackButton />
        <div className="flex items-center gap-2">
          <Gift className="h-5 w-5 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">
            {name ? <Trans k="member.title" values={{ name }} /> : t('member.titleUnknown')}
          </h1>
        </div>
        {buddyOf && (
          <p className="mt-1.5 flex items-start gap-1.5 text-sm text-muted-foreground">
            <Users className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
            <span>
              <Trans k="member.buddyNote" values={{ name: buddyOf.displayName }} />
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
        addLabel={buddyOf ? t('member.addFor', { name: buddyOf.displayName }) : t('editor.addItem')}
        emptyTitle={t('member.empty.title')}
        emptyText={t('member.empty.text')}
        currentUserId={user?.id}
        renderBadge={item => <StatusBadge claim={item.claim} />}
        renderActions={renderActions}
        deleteDescription={deleteDescription}
      />
    </div>
  )
}
