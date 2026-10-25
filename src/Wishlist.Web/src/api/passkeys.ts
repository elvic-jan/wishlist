import client from './client'
import { createPasskey, getPasskey, guessDeviceName } from '@/lib/webauthn'

/**
 * Passkey ceremonies against the API. Each is two calls: options (the server
 * also sets an HttpOnly ceremony cookie) and completion with the browser's
 * credential. The browser dialog runs in between.
 */

type Json = Record<string, unknown>

export interface AuthResult {
  accessToken: string
  user: { id: string; email: string; displayName: string }
  joinedGroupId?: string | null
}

export interface PasskeySummary {
  id: string
  name: string | null
  createdAt: string
  isBackedUp: boolean
}

export interface SecurityOverview {
  emailConfirmed: boolean
  hasPassword: boolean
  passkeys: PasskeySummary[]
}

async function options(path: string, body?: unknown): Promise<Json> {
  const { data } = await client.post<{ options: Json }>(path, body ?? {})
  return data.options
}

/** Modal sign-in ("Sign in with Face ID / fingerprint"), or autofill when `conditional`. */
export async function signInWithPasskey(opts: { conditional?: boolean; signal?: AbortSignal } = {}) {
  const requestOptions = await options('/auth/passkey/login/options')
  const credential = await getPasskey(requestOptions, opts)
  const { data } = await client.post<AuthResult>('/auth/passkey/login', { credential })
  return data
}

export async function signUpWithPasskey(email: string, displayName: string, inviteCode?: string) {
  const details = { email, displayName, inviteCode }
  const creationOptions = await options('/auth/passkey/register/options', details)
  const credential = await createPasskey(creationOptions)
  const { data } = await client.post<AuthResult>('/auth/passkey/register', {
    ...details,
    credential,
    passkeyName: guessDeviceName(),
  })
  return data
}

export async function recoverWithPasskey(email: string, token: string) {
  const creationOptions = await options('/auth/recovery/passkey/options', { email, token })
  const credential = await createPasskey(creationOptions)
  const { data } = await client.post<AuthResult>('/auth/recovery/passkey', {
    email,
    token,
    credential,
    passkeyName: guessDeviceName(),
  })
  return data
}

export async function addPasskey(name = guessDeviceName()) {
  const creationOptions = await options('/auth/passkeys/options')
  const credential = await createPasskey(creationOptions)
  const { data } = await client.post<PasskeySummary>('/auth/passkeys', { credential, name })
  return data
}

export async function getSecurityOverview() {
  const { data } = await client.get<SecurityOverview>('/auth/security')
  return data
}

export async function renamePasskey(id: string, name: string) {
  await client.patch(`/auth/passkeys/${encodeURIComponent(id)}`, { name })
}

export async function removePasskey(id: string) {
  await client.delete(`/auth/passkeys/${encodeURIComponent(id)}`)
}

export async function signOutOtherDevices() {
  await client.post('/auth/sessions/revoke-others')
}

export async function resendEmailConfirmation() {
  await client.post('/auth/confirm-email/resend')
}

export async function confirmEmail(userId: string, token: string) {
  await client.post('/auth/confirm-email', { userId, token })
}

/** The API's error message, if it sent one. */
export function apiErrorMessage(err: unknown): string | undefined {
  return (err as { response?: { data?: { error?: string } } })?.response?.data?.error
}
