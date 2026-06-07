## Why

The SPA today is functionally complete but cosmetically prototype-grade — every page is built with inline `style={{}}` props using hard-coded hex values and raw px sizes, half of `index.css` and all of `App.css` are unused Vite-template scaffolding, the `#root` rule actively fights every page (it sets `width: 1126px; text-align: center;` which every page overrides with its own `maxWidth: 600`), and destructive actions use the native `window.confirm` dialog. There is no app shell — every page hand-rolls its own header with a back-link, and the only UI feedback for errors is a red `<p>` tag. None of the defined CSS custom properties or dark-mode tokens are consumed by any page.

A family-gift-wishlist asks people to share lists with people they care about. The UI needs to feel intentional and inviting, not "we shipped the API and stopped here." This change is the polish pass that turns the existing capabilities into a product.

The change is also forward-looking on two axes:
1. The user wants to **let users choose their own theme** from a list of presets in a future update.
2. The user plans to **move to a micro-frontend architecture** in a future update.

Both are cheap to support *now* by structuring tokens as CSS custom properties on a single root class and by making the app shell own auth + theming so per-page modules can later be swapped in.

## What Changes

- Install **Tailwind CSS** (configured for class-based dark mode and HSL-CSS-var-driven color tokens) and the **shadcn/ui** component primitives we need: `Button`, `Input`, `Label`, `Textarea`, `Card`, `Badge`, `Dialog`, `AlertDialog`, `DropdownMenu`, `Toast`, `Avatar`, `Separator`, `Tooltip`.
- Adopt a **Notion-quiet design language** rendered in a single theme — **Terracotta on warm stone** (orange-700 / orange-400 accent on stone-50/950 neutrals; both light and dark variants).
- Add a **persistent top-nav app shell** wrapping every authenticated page: wordmark (left), mode toggle (Light / Dark / System), user menu (avatar with initials + dropdown with "My wishlist" and "Sign out"). The shell owns auth, theming, and routing — pages slot into a content area below it.
- Introduce a **theme architecture** built around a `theme-<id>` class on `<html>` combined with an optional `dark` class. Today only `theme-terracotta` exists; a future "theme picker" change adds entries to a small registry and the picker UI auto-appears. The current MVP ships hidden picker code paths so the future change is purely additive.
- Add a **first-paint boot script** in `index.html` that reads `localStorage.theme` and `localStorage.mode` and sets the `<html>` classes synchronously before React mounts (no theme/mode FOUC).
- Add a **`prefers-color-scheme` listener** so "System" mode reacts to OS-level theme changes while the tab is open.
- **Rebuild every page** (Login, Register, ForgotPassword, ResetPassword, JoinGroup, Home, GroupDetail, MyWishlist, MemberWishlist) on the new primitives. Pages keep their existing URLs and behavior; only their visual structure and chrome change.
- Replace both `window.confirm` calls (Delete item, Leave group) with shadcn `AlertDialog`.
- Replace the red `<p>` error pattern with `Alert` (form-level) and `Toast` (transient).
- **Clean up Vite-template residue:**
  - Delete `src/Wishlist.Web/src/App.css` entirely (100% dead code).
  - Strip `src/Wishlist.Web/src/index.css` down to the Tailwind directives + the new theme token blocks. Drop the unused tokens (`--code-bg`, `--social-bg`, `--accent-bg`, `--accent-border`, `--shadow`, the `code` styles), the broken `#root` rule, and the old prefers-color-scheme block.
  - Replace `<title>wishlist-web</title>` in `index.html` with `Wishlist`.
  - Replace `/favicon.svg` (Vite default) with an app-appropriate small icon.
  - Replace remaining `err: any` catches across pages with the same typed shape used on the URL pages from the previous PR.

## Capabilities

### New Capabilities

- `ui-foundations`: codifies the theme + mode architecture, the app shell shape, the accessibility minimums (focus visibility, keyboard nav, no `window.confirm`), and the discipline that components consume tokens rather than raw colors. Future "theme picker" / "MFE host shell" / "tenant branding" changes will modify or extend this capability.

### Modified Capabilities

_None._ This change does not alter any product behavior. The wishlists / groups / gift-claiming / user-auth capabilities continue to satisfy their existing requirements; pages are rewritten on new primitives but keep the same URLs, endpoints, and user-visible flows.

## Impact

- **Affected code (every file in `src/Wishlist.Web/src/` is touched in some way):**
  - `src/Wishlist.Web/index.html` — boot script, title, favicon link
  - `src/Wishlist.Web/public/favicon.svg` — new file (replaces Vite default)
  - `src/Wishlist.Web/package.json` — Tailwind, PostCSS, autoprefixer, `tailwindcss-animate`, shadcn deps (Radix primitives, `class-variance-authority`, `clsx`, `tailwind-merge`, `lucide-react`)
  - `src/Wishlist.Web/tailwind.config.ts` — new file
  - `src/Wishlist.Web/postcss.config.js` — new file
  - `src/Wishlist.Web/components.json` — shadcn config (new file)
  - `src/Wishlist.Web/src/index.css` — stripped to Tailwind directives + theme token blocks for `.theme-terracotta` (light) and `.theme-terracotta.dark`
  - `src/Wishlist.Web/src/App.css` — deleted
  - `src/Wishlist.Web/src/lib/utils.ts` — `cn()` helper (shadcn standard)
  - `src/Wishlist.Web/src/components/ui/*.tsx` — shadcn primitives (Button, Input, Label, Textarea, Card, Badge, Dialog, AlertDialog, DropdownMenu, Toast/Toaster, Avatar, Separator, Tooltip, Alert)
  - `src/Wishlist.Web/src/themes/registry.ts` — new file: the future-proofing theme registry (one entry today)
  - `src/Wishlist.Web/src/context/ThemeContext.tsx` — new file: theme + mode state, persistence, system-preference listener
  - `src/Wishlist.Web/src/components/AppShell.tsx` — new file: top nav, mode toggle, user menu
  - `src/Wishlist.Web/src/App.tsx` — routing rewrap so authenticated routes render inside `<AppShell>`
  - All 9 page components — rebuilt on the new primitives
- **APIs**: no change.
- **Dependencies (frontend, new)**:
  - `tailwindcss`, `postcss`, `autoprefixer`, `tailwindcss-animate`
  - `class-variance-authority`, `clsx`, `tailwind-merge`
  - `lucide-react` (icon set; used by shadcn primitives and the mode toggle)
  - Radix UI primitive packages pulled by individual shadcn components: `@radix-ui/react-dialog`, `@radix-ui/react-dropdown-menu`, `@radix-ui/react-avatar`, `@radix-ui/react-label`, `@radix-ui/react-separator`, `@radix-ui/react-slot`, `@radix-ui/react-toast`, `@radix-ui/react-tooltip`
- **No backend, no DB, no API contract, no routing-URL changes.**
- **Out of scope:** the theme picker UI itself, per-user theme persistence to the DB, motion beyond shadcn's built-ins, i18n, copy rewrites, micro-frontend extraction. Those are explicitly future changes; the current architecture is set up so they are additive.
