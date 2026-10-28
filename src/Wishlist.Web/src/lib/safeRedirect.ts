/**
 * Where to go after sign-in: the given path if it stays on this site, else the home page.
 * Browsers read "/\host" and "//host" as another site, so those are refused before the
 * value is resolved; anything that then resolves to another origin is refused too.
 */
export function safeRedirect(value: string | null, origin = window.location.origin): string {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.includes('\\')) return '/'
  try {
    const url = new URL(value, origin)
    return url.origin === origin ? url.pathname + url.search + url.hash : '/'
  } catch {
    return '/'
  }
}
