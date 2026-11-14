import { apiErrorMessage, apiErrorStatus } from '@/api/errors'
import { t } from '@/lib/i18n'

/**
 * Turns an API error from an invite-code request (register or join) into a
 * message the invitee can act on.
 */
export function inviteErrorMessage(err: unknown, fallback: string): string {
  const message = apiErrorMessage(err) ?? fallback

  switch (apiErrorStatus(err)) {
    case 404:
    case 410:
      return t('invite.error.askNew', { message })
    case 429:
      return t('invite.error.tooMany')
    default:
      return message
  }
}
