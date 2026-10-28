// The API answers failures with { "error": "<message for people>" }.

type ApiFailure = { response?: { status?: number; data?: { error?: string } } }

/** The API's error message, if it sent one. */
export function apiErrorMessage(err: unknown): string | undefined {
  return (err as ApiFailure)?.response?.data?.error
}

/** The HTTP status of a failed API call, if there was a response. */
export function apiErrorStatus(err: unknown): number | undefined {
  return (err as ApiFailure)?.response?.status
}
