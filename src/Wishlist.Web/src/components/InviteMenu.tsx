import { useState } from 'react'
import { Copy, Loader2, Mail, MessageCircle, Share2, UserPlus, Users } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { useToast } from '@/hooks/use-toast'
import client from '@/api/client'
import { buildInviteMessage } from '@/lib/inviteMessage'
import { InviteFromCirclesDialog } from '@/components/InviteFromCirclesDialog'

interface Invitation {
  inviteUrl: string
  expiresAt: string
}

const canShareNatively = typeof navigator !== 'undefined' && typeof navigator.share === 'function'

/**
 * "Invite people": opening the menu creates a new invite link, then the
 * member picks how to send it. Every option carries the same message.
 */
export function InviteMenu({
  groupId,
  circleName,
  inviterName,
  onLinkCreated,
}: {
  groupId: string
  circleName: string
  inviterName: string
  /** Lets the page show the latest link, for copying by hand. */
  onLinkCreated?: (url: string) => void
}) {
  const { toast } = useToast()
  const [invitation, setInvitation] = useState<Invitation | null>(null)
  const [pickerOpen, setPickerOpen] = useState(false)

  const createLink = async () => {
    setInvitation(null)
    try {
      const { data } = await client.post<Invitation>(`/groups/${groupId}/invitations`)
      setInvitation(data)
      onLinkCreated?.(data.inviteUrl)
    } catch {
      toast({ title: 'Couldn’t create an invite link', variant: 'destructive' })
    }
  }

  const message = invitation
    ? buildInviteMessage(inviterName, circleName, invitation.inviteUrl, invitation.expiresAt)
    : null

  const share = async () => {
    if (!message || !invitation) return
    try {
      await navigator.share({ title: message.subject, text: message.text, url: invitation.inviteUrl })
    } catch (err) {
      // Closing the share sheet is not an error.
      if ((err as DOMException)?.name !== 'AbortError')
        toast({ title: 'Couldn’t open sharing', description: 'Try WhatsApp, Email or Copy link.', variant: 'destructive' })
    }
  }

  const copy = async () => {
    if (!invitation) return
    try {
      await navigator.clipboard.writeText(invitation.inviteUrl)
      toast({ title: 'Invite link copied', description: 'Send it to whoever you want to add.' })
    } catch {
      toast({ title: 'Couldn’t copy the link', description: 'Select the link below and copy it.', variant: 'destructive' })
    }
  }

  const ready = invitation !== null

  return (
    <>
      <DropdownMenu onOpenChange={open => open && createLink()}>
        <DropdownMenuTrigger asChild>
          <Button variant="outline">
            <UserPlus className="h-4 w-4" />
            Invite people
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-56">
          <DropdownMenuItem onSelect={() => setPickerOpen(true)}>
            <Users className="h-4 w-4" />
            People from my circles
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuLabel className="flex items-center gap-2 text-xs font-normal text-muted-foreground">
            {ready ? (
              'Send a new invite link'
            ) : (
              <>
                <Loader2 className="h-3 w-3 animate-spin" aria-hidden />
                Creating invite link…
              </>
            )}
          </DropdownMenuLabel>
          <DropdownMenuSeparator />
          {canShareNatively && (
            <DropdownMenuItem disabled={!ready} onSelect={share}>
              <Share2 className="h-4 w-4" />
              Share…
            </DropdownMenuItem>
          )}
          <DropdownMenuItem
            disabled={!ready}
            onSelect={() => message && window.open(message.whatsAppUrl, '_blank', 'noopener')}
          >
            <MessageCircle className="h-4 w-4" />
            WhatsApp
          </DropdownMenuItem>
          <DropdownMenuItem
            disabled={!ready}
            onSelect={() => {
              if (message) window.location.href = message.mailtoUrl
            }}
          >
            <Mail className="h-4 w-4" />
            Email
          </DropdownMenuItem>
          <DropdownMenuItem disabled={!ready} onSelect={copy}>
            <Copy className="h-4 w-4" />
            Copy link
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
      <InviteFromCirclesDialog
        groupId={groupId}
        circleName={circleName}
        open={pickerOpen}
        onOpenChange={setPickerOpen}
      />
    </>
  )
}
