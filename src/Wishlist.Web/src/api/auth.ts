import client from './client';

/**
 * Triggers a password-reset email if the address belongs to a registered user.
 *
 * The server always responds 202 Accepted regardless of whether the email
 * exists, so this function never throws on "not registered" — only on
 * transport failures or 429 rate-limit responses.
 */
export async function requestPasswordReset(email: string): Promise<void> {
  await client.post('/auth/forgot-password', { email });
}

/**
 * Redeems a password-reset token from the email link and sets a new password.
 *
 * On success the server returns 204 No Content (and revokes all existing
 * sessions for the user). On any failure — bad/expired/replayed token, weak
 * new password — the server returns 400 with a generic message; axios will
 * reject this Promise with that error.
 */
export async function resetPassword(
  email: string,
  token: string,
  newPassword: string,
): Promise<void> {
  await client.post('/auth/reset-password', { email, token, newPassword });
}
