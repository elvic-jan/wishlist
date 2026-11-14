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
import { useT } from '@/lib/i18n'
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
  const t = useT()
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
      toast({ title: t('inviteMenu.linkFailed'), variant: 'destructive' })
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
        toast({ title: t('inviteMenu.shareFailed'), description: t('inviteMenu.shareFailedText'), variant: 'destructive' })
    }
  }

  const copy = async () => {
    if (!invitation) return
    try {
      await navigator.clipboard.writeText(invitation.inviteUrl)
      toast({ title: t('inviteMenu.copied'), description: t('inviteMenu.copiedText') })
    } catch {
      toast({ title: t('inviteMenu.copyFailed'), description: t('inviteMenu.copyFailedText'), variant: 'destructive' })
    }
  }

  const ready = invitation !== null

  return (
    <>
      <DropdownMenu onOpenChange={open => open && createLink()}>
        <DropdownMenuTrigger asChild>
          <Button variant="outline">
            <UserPlus className="h-4 w-4" />
            {t('inviteMenu.invite')}
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-56">
          <DropdownMenuItem onSelect={() => setPickerOpen(true)}>
            <Users className="h-4 w-4" />
            {t('inviteMenu.fromCircles')}
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuLabel className="flex items-center gap-2 text-xs font-normal text-muted-foreground">
            {ready ? (
              t('inviteMenu.sendLink')
            ) : (
              <>
                <Loader2 className="h-3 w-3 animate-spin" aria-hidden />
                {t('inviteMenu.creating')}
              </>
            )}
          </DropdownMenuLabel>
          <DropdownMenuSeparator />
          {canShareNatively && (
            <DropdownMenuItem disabled={!ready} onSelect={share}>
              <Share2 className="h-4 w-4" />
              {t('inviteMenu.share')}
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
            {t('inviteMenu.email')}
          </DropdownMenuItem>
          <DropdownMenuItem disabled={!ready} onSelect={copy}>
            <Copy className="h-4 w-4" />
            {t('inviteMenu.copy')}
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
