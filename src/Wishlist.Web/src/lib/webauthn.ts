/**
 * Thin helpers around navigator.credentials for passkeys.
 *
 * The API sends WebAuthn options as JSON (binary fields base64url-encoded) and
 * expects the credential back as JSON. Newer browsers convert both ways
 * themselves (PublicKeyCredential.parse*OptionsFromJSON / toJSON); older ones
 * get the manual conversion below.
 */

type Json = Record<string, unknown>

/** Whether this browser can create and use passkeys at all. */
export function passkeysSupported(): boolean {
  return typeof window !== 'undefined' && typeof window.PublicKeyCredential === 'function'
}

/** Whether the browser can offer passkeys in the email field's autofill. */
export async function passkeyAutofillSupported(): Promise<boolean> {
  if (!passkeysSupported()) return false
  const pkc = window.PublicKeyCredential as unknown as {
    isConditionalMediationAvailable?: () => Promise<boolean>
  }
  try {
    return (await pkc.isConditionalMediationAvailable?.()) ?? false
  } catch {
    return false
  }
}

/** Runs navigator.credentials.create() for creation options from the API. */
export async function createPasskey(options: Json): Promise<Json> {
  const publicKey = parseCreationOptions(options)
  const credential = (await navigator.credentials.create({ publicKey })) as PublicKeyCredential | null
  if (!credential) throw new DOMException('No passkey was created.', 'NotAllowedError')
  return credentialToJson(credential)
}

/**
 * Runs navigator.credentials.get() for request options from the API.
 * With `conditional`, the browser waits quietly and offers passkeys in the
 * autofill of an input with autocomplete="… webauthn".
 */
export async function getPasskey(
  options: Json,
  { conditional = false, signal }: { conditional?: boolean; signal?: AbortSignal } = {},
): Promise<Json> {
  const publicKey = parseRequestOptions(options)
  const credential = (await navigator.credentials.get({
    publicKey,
    signal,
    ...(conditional ? { mediation: 'conditional' as CredentialMediationRequirement } : {}),
  })) as PublicKeyCredential | null
  if (!credential) throw new DOMException('No passkey was chosen.', 'NotAllowedError')
  return credentialToJson(credential)
}

/** The person closed the dialog, it timed out, or another request took over. */
export function isPasskeyCancelled(err: unknown): boolean {
  return err instanceof DOMException && (err.name === 'NotAllowedError' || err.name === 'AbortError')
}

/**
 * A plain-language error for a failed browser passkey request, or null when the
 * person simply cancelled.
 */
export function passkeyErrorMessage(err: unknown): string | null {
  if (isPasskeyCancelled(err)) return null
  if (err instanceof DOMException) {
    if (err.name === 'InvalidStateError')
      return 'This device already has a passkey for your account.'
    if (err.name === 'SecurityError')
      return 'Face ID / fingerprint sign-in isn’t available on this web address. Open GiftCircles at giftcircles.app and try again.'
    if (err.name === 'NotSupportedError')
      return 'This browser can’t use Face ID or fingerprint sign-in. Use your password instead.'
  }
  return null
}

/** A label for a new passkey, so the security page can tell devices apart. */
export function guessDeviceName(): string {
  const ua = navigator.userAgent
  if (/iPhone/.test(ua)) return 'iPhone'
  if (/iPad/.test(ua)) return 'iPad'
  if (/Android/.test(ua)) return 'Android phone'
  if (/Macintosh/.test(ua)) return navigator.maxTouchPoints > 1 ? 'iPad' : 'Mac'
  if (/Windows/.test(ua)) return 'Windows PC'
  if (/CrOS/.test(ua)) return 'Chromebook'
  if (/Linux/.test(ua)) return 'Linux PC'
  return 'This device'
}

// ---- conversion -------------------------------------------------------------

type PkcStatics = {
  parseCreationOptionsFromJSON?: (o: Json) => PublicKeyCredentialCreationOptions
  parseRequestOptionsFromJSON?: (o: Json) => PublicKeyCredentialRequestOptions
}

function statics(): PkcStatics {
  return window.PublicKeyCredential as unknown as PkcStatics
}

function parseCreationOptions(options: Json): PublicKeyCredentialCreationOptions {
  const native = statics().parseCreationOptionsFromJSON
  if (native) return native(options)

  const user = options.user as Json
  return {
    ...(options as unknown as PublicKeyCredentialCreationOptions),
    challenge: fromBase64Url(options.challenge as string),
    user: { ...(user as unknown as PublicKeyCredentialUserEntity), id: fromBase64Url(user.id as string) },
    excludeCredentials: ((options.excludeCredentials as Json[] | undefined) ?? []).map(descriptor),
  }
}

function parseRequestOptions(options: Json): PublicKeyCredentialRequestOptions {
  const native = statics().parseRequestOptionsFromJSON
  if (native) return native(options)

  return {
    ...(options as unknown as PublicKeyCredentialRequestOptions),
    challenge: fromBase64Url(options.challenge as string),
    allowCredentials: ((options.allowCredentials as Json[] | undefined) ?? []).map(descriptor),
  }
}

function descriptor(d: Json): PublicKeyCredentialDescriptor {
  return {
    ...(d as unknown as PublicKeyCredentialDescriptor),
    id: fromBase64Url(d.id as string),
  }
}

function credentialToJson(credential: PublicKeyCredential): Json {
  const withToJson = credential as unknown as { toJSON?: () => Json }
  if (typeof withToJson.toJSON === 'function') {
    try {
      return withToJson.toJSON()
    } catch {
      // Some password-manager extensions return objects whose toJSON throws.
    }
  }

  const response = credential.response
  const json: Json = {
    id: credential.id,
    rawId: toBase64Url(credential.rawId),
    type: credential.type,
    authenticatorAttachment: credential.authenticatorAttachment ?? undefined,
    clientExtensionResults: credential.getClientExtensionResults(),
  }

  if ('attestationObject' in response) {
    const attestation = response as AuthenticatorAttestationResponse
    json.response = {
      clientDataJSON: toBase64Url(attestation.clientDataJSON),
      attestationObject: toBase64Url(attestation.attestationObject),
      transports: attestation.getTransports?.() ?? [],
    }
  } else {
    const assertion = response as AuthenticatorAssertionResponse
    json.response = {
      clientDataJSON: toBase64Url(assertion.clientDataJSON),
      authenticatorData: toBase64Url(assertion.authenticatorData),
      signature: toBase64Url(assertion.signature),
      userHandle: assertion.userHandle ? toBase64Url(assertion.userHandle) : undefined,
    }
  }
  return json
}

function fromBase64Url(value: string): ArrayBuffer {
  const base64 = value.replace(/-/g, '+').replace(/_/g, '/')
  const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4)
  const binary = atob(padded)
  const bytes = new Uint8Array(binary.length)
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i)
  return bytes.buffer
}

function toBase64Url(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer)
  let binary = ''
  for (const b of bytes) binary += String.fromCharCode(b)
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}
