/**
 * The invitation text every share option uses (share sheet, WhatsApp, email),
 * so people get the same friendly message whichever way it's sent.
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
  const subject = `Join my ${circleName} circle on GiftCircles`
  const text =
    `${inviterName} invited you to the ${circleName} circle on GiftCircles, where we share wishlists. ` +
    `The link works for ${days} ${days === 1 ? 'day' : 'days'}:`
  const textWithLink = `${text} ${url}`

  return {
    subject,
    text,
    textWithLink,
    whatsAppUrl: `https://wa.me/?text=${encodeURIComponent(textWithLink)}`,
    mailtoUrl: `mailto:?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(textWithLink)}`,
  }
}
