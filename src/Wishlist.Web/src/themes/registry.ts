/**
 * Theme registry — the single source of truth for available themes.
 *
 * Each entry describes one theme that ships in the SPA. The `id` is the
 * stable kebab-case identifier (used as the suffix of the `theme-<id>`
 * CSS class and as the `localStorage.theme` value). The `displayName` is
 * the user-visible label that a future theme picker will surface.
 *
 * To add a new theme:
 *  1. Define `.theme-<id>` and `.theme-<id>.dark` blocks in `src/index.css`.
 *  2. Add an entry below.
 *  3. Add the new id to the `ThemeId` union.
 *
 * No other application code change is required — the boot script,
 * ThemeContext, and the (future) picker UI all read from this registry.
 */

export type ThemeId = 'terracotta'

export interface ThemeMeta {
  displayName: string
  /** A representative color the future picker will render as a swatch. */
  swatch: string
}

export const themes: Record<ThemeId, ThemeMeta> = {
  terracotta: { displayName: 'Earth', swatch: '#c2410c' },
}

export const themeIds = Object.keys(themes) as ThemeId[]

export const DEFAULT_THEME: ThemeId = 'terracotta'

export type Mode = 'light' | 'dark' | 'system'
export const DEFAULT_MODE: Mode = 'system'

/**
 * Storage-key constants. These strings are duplicated by the inline boot
 * script in `index.html` — keep them in lockstep.
 */
export const STORAGE_KEY_THEME = 'theme'
export const STORAGE_KEY_MODE = 'mode'
