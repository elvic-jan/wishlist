import { useEffect, useState } from 'react'
import client from '@/api/client'
import { inviteErrorMessage } from '@/lib/inviteErrors'
import { t } from '@/lib/i18n'

export interface ValidInvitation {
  state: 'valid'
  inviterName: string
  circleName: string
  /** A circle's shared link, or someone's personal Secret Santa invitation. */
  kind: 'circle' | 'drawing'
  /** For a drawing: the name and address the organizer typed. */
  name: string | null
  email: string | null
  /** The drawing invitation belongs to an existing account: answer it by signing in. */
  signInRequired: boolean
}

export type InvitationCheck =
  | { state: 'checking' }
  | ValidInvitation
  | { state: 'invalid'; message: string }

type CheckResponse = Omit<ValidInvitation, 'state' | 'kind'> & { kind?: 'circle' | 'drawing' }

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
      .get<CheckResponse>(`/invitations/${code}`)
      .then(({ data }) => {
        if (!cancelled)
          setResult({
            code,
            check: {
              state: 'valid',
              inviterName: data.inviterName,
              circleName: data.circleName,
              kind: data.kind ?? 'circle',
              name: data.name ?? null,
              email: data.email ?? null,
              signInRequired: data.signInRequired ?? false,
            },
          })
      })
      .catch((err: unknown) => {
        if (!cancelled)
          setResult({
            code,
            check: { state: 'invalid', message: inviteErrorMessage(err, t('invite.error.broken')) },
          })
      })
    return () => {
      cancelled = true
    }
  }, [code])

  // A result for a previous code is stale until the new check finishes.
  return result && result.code === code ? result.check : { state: 'checking' }
}
