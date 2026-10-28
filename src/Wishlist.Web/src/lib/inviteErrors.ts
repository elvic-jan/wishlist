import { apiErrorMessage, apiErrorStatus } from '@/api/errors'

/**
 * Turns an API error from an invite-code request (register or join) into a
 * message the invitee can act on.
 */
export function inviteErrorMessage(err: unknown, fallback: string): string {
  const message = apiErrorMessage(err) ?? fallback

  switch (apiErrorStatus(err)) {
    case 404:
    case 410:
      return `${message} Ask the person who invited you for a new link.`
    case 429:
      return 'Too many invitation attempts. Please wait a few minutes and try again.'
    default:
      return message
  }
}
