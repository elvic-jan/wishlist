/**
 * Turns an API error from an invite-code request (register or join) into a
 * message the invitee can act on.
 */
export function inviteErrorMessage(err: unknown, fallback: string): string {
  const response = (err as { response?: { status?: number; data?: { error?: string } } })
    ?.response
  const message = response?.data?.error ?? fallback

  switch (response?.status) {
    case 404:
    case 410:
      return `${message} Ask the person who invited you for a new link.`
    case 429:
      return 'Too many invitation attempts. Please wait a few minutes and try again.'
    default:
      return message
  }
}
