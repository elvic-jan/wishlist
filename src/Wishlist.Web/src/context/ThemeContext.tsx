import {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import {
  DEFAULT_MODE,
  DEFAULT_THEME,
  STORAGE_KEY_MODE,
  STORAGE_KEY_THEME,
  themeIds,
  type Mode,
  type ThemeId,
} from '@/themes/registry'

export interface ThemeContextValue {
  /** Active theme id (e.g. 'terracotta'). */
  theme: ThemeId
  /** User's mode preference. */
  mode: Mode
  /** The actual rendered mode (resolves 'system' to light/dark). */
  resolvedMode: 'light' | 'dark'
  setTheme: (id: ThemeId) => void
  setMode: (mode: Mode) => void
}

// eslint-disable-next-line react-refresh/only-export-components -- context object co-located with provider; HMR caveat is acceptable.
export const ThemeContext = createContext<ThemeContextValue | null>(null)

function readStoredTheme(): ThemeId {
  try {
    const raw = localStorage.getItem(STORAGE_KEY_THEME)
    if (raw && (themeIds as string[]).includes(raw)) return raw as ThemeId
  } catch {
    // localStorage unavailable; fall through.
  }
  return DEFAULT_THEME
}

function readStoredMode(): Mode {
  try {
    const raw = localStorage.getItem(STORAGE_KEY_MODE)
    if (raw === 'light' || raw === 'dark' || raw === 'system') return raw
  } catch {
    // ignore
  }
  return DEFAULT_MODE
}

function systemPrefersDark(): boolean {
  return (
    typeof window !== 'undefined' &&
    window.matchMedia('(prefers-color-scheme: dark)').matches
  )
}

function applyClasses(theme: ThemeId, dark: boolean) {
  const root = document.documentElement
  // Remove any previously applied theme-* class.
  root.classList.forEach(cls => {
    if (cls.startsWith('theme-')) root.classList.remove(cls)
  })
  root.classList.add(`theme-${theme}`)
  if (dark) root.classList.add('dark')
  else root.classList.remove('dark')
  syncBrowserColors()
}

/**
 * Lets the browser's own bars and controls follow the app's mode, not only the
 * phone's setting: the theme-color meta gets the page background of the active
 * theme, and color-scheme tells the browser the page is light or dark.
 */
function syncBrowserColors() {
  const meta = document.querySelector('meta[name="theme-color"]')
  const background = getComputedStyle(document.body).backgroundColor
  if (meta && background) meta.setAttribute('content', background)
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<ThemeId>(() => readStoredTheme())
  const [mode, setModeState] = useState<Mode>(() => readStoredMode())
  const [systemDark, setSystemDark] = useState<boolean>(() => systemPrefersDark())

  const resolvedMode: 'light' | 'dark' =
    mode === 'system' ? (systemDark ? 'dark' : 'light') : mode

  // Apply classes any time theme or resolved mode changes.
  useEffect(() => {
    applyClasses(theme, resolvedMode === 'dark')
  }, [theme, resolvedMode])

  // Track the OS preference; only honored when mode === 'system'.
  useEffect(() => {
    if (typeof window === 'undefined') return
    const mql = window.matchMedia('(prefers-color-scheme: dark)')
    const handler = (e: MediaQueryListEvent) => setSystemDark(e.matches)
    mql.addEventListener('change', handler)
    return () => mql.removeEventListener('change', handler)
  }, [])

  const setTheme = useCallback((next: ThemeId) => {
    try {
      localStorage.setItem(STORAGE_KEY_THEME, next)
    } catch {
      // ignore
    }
    setThemeState(next)
  }, [])

  const setMode = useCallback((next: Mode) => {
    try {
      localStorage.setItem(STORAGE_KEY_MODE, next)
    } catch {
      // ignore
    }
    setModeState(next)
  }, [])

  const value = useMemo<ThemeContextValue>(
    () => ({ theme, mode, resolvedMode, setTheme, setMode }),
    [theme, mode, resolvedMode, setTheme, setMode],
  )

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}
