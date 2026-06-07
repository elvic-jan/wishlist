import { useEffect, useState } from 'react'
import { ArrowLeft, Check, Copy, LogOut, Users } from 'lucide-react'
import { Link, useNavigate, useParams } from 'react-router-dom'
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
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { useToast } from '@/hooks/use-toast'
import client from '@/api/client'
import { useAuth } from '@/context/AuthContext'

interface Member {
  userId: string
  displayName: string
  role: string
}

interface WishlistEntry {
  userId: string
  displayName: string
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/).slice(0, 2)
  return parts.map(p => p[0]?.toUpperCase() ?? '').join('') || '?'
}

export function GroupDetailPage() {
  const { groupId } = useParams<{ groupId: string }>()
  const { user } = useAuth()
  const navigate = useNavigate()
  const { toast } = useToast()
  const [members, setMembers] = useState<Member[]>([])
  const [wishlists, setWishlists] = useState<WishlistEntry[]>([])
  const [inviteUrl, setInviteUrl] = useState('')
  const [copied, setCopied] = useState(false)
  const [error, setError] = useState('')
  const [confirmLeave, setConfirmLeave] = useState(false)

  useEffect(() => {
    Promise.all([
      client.get(`/groups/${groupId}/members`),
      client.get(`/groups/${groupId}/wishlists`),
    ])
      .then(([membersRes, wishlistsRes]) => {
        setMembers(membersRes.data)
        setWishlists(wishlistsRes.data)
      })
      .catch(() => setError('Failed to load group details.'))
  }, [groupId])

  const generateInvite = async () => {
    try {
      const { data } = await client.post(`/groups/${groupId}/invitations`)
      setInviteUrl(data.inviteUrl)
      await navigator.clipboard.writeText(data.inviteUrl)
      setCopied(true)
      toast({
        title: 'Invite link copied',
        description: 'Send it to whoever you want to add.',
      })
      setTimeout(() => setCopied(false), 3000)
    } catch {
      setError('Failed to generate invitation link.')
    }
  }

  const leaveGroup = async () => {
    try {
      await client.delete(`/groups/${groupId}/members/me`)
      navigate('/')
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error
      setError(msg ?? 'Failed to leave group.')
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
          <Users className="h-5 w-5 text-primary" aria-hidden />
          <h1 className="text-2xl font-semibold tracking-tight">Group</h1>
        </div>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Members</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2">
          {members.map(m => (
            <div
              key={m.userId}
              className="flex items-center justify-between gap-3 py-1"
            >
              <div className="flex items-center gap-3">
                <Avatar className="h-8 w-8">
                  <AvatarFallback className="bg-secondary text-secondary-foreground text-xs">
                    {initials(m.displayName)}
                  </AvatarFallback>
                </Avatar>
                <span className="text-sm font-medium">{m.displayName}</span>
                {m.role === 'Owner' && (
                  <Badge variant="secondary" className="text-xs font-normal">
                    Owner
                  </Badge>
                )}
              </div>
              {m.userId !== user?.id && (
                <Button asChild variant="ghost" size="sm">
                  <Link to={`/wishlist/${m.userId}`}>View wishlist</Link>
                </Button>
              )}
            </div>
          ))}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Other wishlists</CardTitle>
        </CardHeader>
        <CardContent>
          {wishlists.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No other wishlists in this group yet.
            </p>
          ) : (
            <ul className="space-y-2">
              {wishlists.map(w => (
                <li key={w.userId}>
                  <Link to={`/wishlist/${w.userId}`} className="block">
                    <div className="flex items-center justify-between rounded-md border px-3 py-2 text-sm transition-colors hover:bg-accent/40">
                      <span>{w.displayName}&rsquo;s wishlist</span>
                      <span className="text-muted-foreground">→</span>
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Separator />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <Button onClick={generateInvite} variant="outline">
          {copied ? (
            <Check className="h-4 w-4" />
          ) : (
            <Copy className="h-4 w-4" />
          )}
          {copied ? 'Copied!' : 'Generate invite link'}
        </Button>
        <Button
          onClick={() => setConfirmLeave(true)}
          variant="ghost"
          className="text-destructive hover:bg-destructive/10 hover:text-destructive"
        >
          <LogOut className="h-4 w-4" />
          Leave group
        </Button>
      </div>

      {inviteUrl && (
        <p className="break-all rounded-md border bg-muted/50 p-3 text-xs text-muted-foreground">
          {inviteUrl}
        </p>
      )}

      <AlertDialog open={confirmLeave} onOpenChange={setConfirmLeave}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Leave this group?</AlertDialogTitle>
            <AlertDialogDescription>
              You&rsquo;ll lose access to other members&rsquo; wishlists. You
              can rejoin later if someone shares the invite link with you
              again.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={leaveGroup}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Leave group
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
