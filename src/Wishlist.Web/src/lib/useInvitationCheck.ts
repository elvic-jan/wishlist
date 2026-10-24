import { useEffect, useState } from 'react'
import client from '@/api/client'
import { inviteErrorMessage } from '@/lib/inviteErrors'

export type InvitationCheck =
  | { state: 'checking' }
  | { state: 'valid' }
  | { state: 'invalid'; message: string }

/**
 * Validates an invite code up front, so a visitor with a dead link is told
 * before they fill in the sign-up form. Pass `undefined` to skip the check.
 */
export function useInvitationCheck(code: string | undefined): InvitationCheck {
  const [result, setResult] = useState<{ code: string; check: InvitationCheck } | null>(null)

  useEffect(() => {
    if (!code) return
    let cancelled = false
    client
      .get(`/invitations/${code}`)
      .then(() => {
        if (!cancelled) setResult({ code, check: { state: 'valid' } })
      })
      .catch((err: unknown) => {
        if (!cancelled)
          setResult({
            code,
            check: { state: 'invalid', message: inviteErrorMessage(err, 'This invitation link doesn’t work.') },
          })
      })
    return () => {
      cancelled = true
    }
  }, [code])

  // A result for a previous code is stale until the new check finishes.
  return result && result.code === code ? result.check : { state: 'checking' }
}
