import { plural, t } from '@/lib/i18n'

/**
 * The invitation text every share option uses (share sheet, WhatsApp, email),
 * so people get the same friendly message whichever way it's sent. Written in
 * the language the inviter is using.
 */
export interface InviteMessage {
  /** Title for the share sheet and the email subject. */
  subject: string
  /** The message without the link (the share sheet adds the link itself). */
  text: string
  /** The message followed by the link, for WhatsApp, email and copying. */
  textWithLink: string
  whatsAppUrl: string
  mailtoUrl: string
}

function daysLeft(expiresAt: string, now = Date.now()): number {
  return Math.max(1, Math.round((new Date(expiresAt).getTime() - now) / 86_400_000))
}

export function buildInviteMessage(
  inviterName: string,
  circleName: string,
  url: string,
  expiresAt: string,
  now = Date.now(),
): InviteMessage {
  const days = daysLeft(expiresAt, now)
  const subject = t('invite.share.subject', { circle: circleName })
  const text = plural('invite.share.text', days, { inviter: inviterName, circle: circleName })
  const textWithLink = `${text} ${url}`

  return {
    subject,
    text,
    textWithLink,
    whatsAppUrl: `https://wa.me/?text=${encodeURIComponent(textWithLink)}`,
    mailtoUrl: `mailto:?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(textWithLink)}`,
  }
}
