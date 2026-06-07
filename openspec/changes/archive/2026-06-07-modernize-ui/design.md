## Context

The SPA is a Vite + React 19 application using React Router v7 for routing and an axios client with a JWT-refresh interceptor for API calls. There is no CSS framework, no component library, no design system, and no app shell — every page is built top-to-bottom with inline `style={{}}` props using hard-coded hex colors and px values. Two CSS files exist (`index.css`, `App.css`); the former defines unused tokens and a hostile `#root` rule that fights every page, the latter is entirely dead Vite-template code. The wishlist application's only "design language" today is therefore *whatever the default browser stylesheet renders, minus a little bit of inline tweaking*.

The product is a family-gift-wishlist: people share lists, claim gifts for each other, see status. Content-density is low (a handful of items per list, a handful of groups per user), reads happen far more than writes, and the emotional register is intimate — not "power user" but "share with loved ones." That argues for a calm, content-first design rather than a dashboard-dense one.

Two forward-looking constraints shape this change:

1. **Theme picker (future):** users will eventually pick from a list of predefined themes. So the architecture has to treat "theme" as a first-class identifier, not a hardcoded set of values baked into the stylesheet.
2. **Micro-frontends (future):** different parts of the app may be served by separate modules. So the app shell has to own auth and theming, and per-page modules have to be free to slot in below the shell without needing to re-implement those concerns.

## Goals / Non-Goals

**Goals:**
- Every page reads "clean and intentional" — Notion-quiet design language, generous spacing, soft cards, low-chrome typography, single accent.
- The visual system is fully token-driven so a future theme picker is purely additive.
- Dark mode works correctly and automatically (System preference + manual override).
- All destructive actions use accessible dialogs (no `window.confirm`).
- Keyboard navigation and focus visibility are correct on every interactive element.
- The app shell is the stable host across pages, ready to become an MFE host later.
- Vite-template residue is removed.

**Non-Goals:**
- The theme picker UI itself, or per-user theme persistence to the database. The infrastructure ships ready; the UI does not.
- Micro-frontend extraction. The shell is *structured* MFE-friendly but remains a monolithic SPA in this change.
- New product capabilities. The pages do the same things, just better.
- Custom illustrations, brand identity, or marketing copy.
- Motion design beyond shadcn's built-in transitions (open/close on dialogs, hover on cards).
- A logged-out marketing/landing page.
- Accessibility audit beyond shadcn's built-ins (we get a lot for free via Radix; a formal WCAG sweep is out of scope).
- i18n / localization.

## Decisions

### Decision 1: Tailwind CSS with class-based dark mode, configured to consume CSS-var tokens

Adopt Tailwind v3+ with `darkMode: ['class', '.dark']` and color tokens defined as `hsl(var(--token))` so opacity utilities (`bg-primary/50`) still work. This is the standard shadcn pattern; we adopt it for compatibility and because it cleanly composes with the multi-theme requirement (a theme is a class scope; dark mode is another class on the same root).

The Tailwind color palette is *augmented* (not replaced) with semantic tokens:
- `background`, `foreground`
- `card`, `card-foreground`
- `popover`, `popover-foreground`
- `primary`, `primary-foreground`
- `secondary`, `secondary-foreground`
- `muted`, `muted-foreground`
- `accent`, `accent-foreground` (interactive hover/highlighted state)
- `destructive`, `destructive-foreground`
- `border`, `input`, `ring`

Component code uses *only* these tokens. Direct Tailwind color utilities like `bg-orange-700` or `text-stone-500` are not used in app code (only allowed in the token sheet itself).

**Alternatives considered:**
- *Raw Tailwind with hand-picked colors per component:* Rejected. Locks in one theme forever; future theme picker has to refactor every component.
- *CSS Modules + handroll:* Rejected. Component-by-component velocity is much lower than shadcn primitives, and accessibility correctness is harder to achieve from scratch.
- *Mantine / Chakra:* Rejected. Heavier dependencies, distinct "library look," and theming is JS-based which is awkward to expose to future MFEs.

### Decision 2: shadcn/ui for component primitives

Use shadcn/ui — accessible Radix-based primitives copy-pasted into our repo (not a runtime package). We get keyboard navigation, focus management, ARIA correctness, and a coherent style baseline for free. Because the code lives in our repo we can edit any primitive freely without forking a package.

Primitives in scope for this change:
- `Button` (variants: default, secondary, outline, ghost, destructive, link; sizes: default, sm, lg, icon)
- `Input`, `Label`, `Textarea`
- `Card` (with `CardHeader` / `CardTitle` / `CardDescription` / `CardContent` / `CardFooter`)
- `Badge` (variants: default, secondary, outline, destructive)
- `Dialog`, `AlertDialog`
- `DropdownMenu`
- `Toast` / `Toaster` (form-level transient feedback)
- `Avatar` (initials fallback)
- `Separator`
- `Tooltip`
- `Alert` (form-level error display)

We pull shadcn primitives one at a time via the CLI (or by copy-paste from the upstream repo); we do NOT bulk-install every primitive shadcn has.

**Alternatives considered:**
- *Headless UI (Radix directly, no shadcn):* Rejected. shadcn IS Radix + Tailwind glue; rebuilding the glue ourselves is busywork.
- *DaisyUI / Park UI:* Rejected. We want to own the styles so theme work is local; pluggable design systems get in the way.

### Decision 3: Theme architecture — class composition on `<html>`

`<html>` carries two orthogonal classes:

```
<html class="theme-terracotta dark">
       │                    │
       │                    └─ activates dark variants of the active theme
       └─ scopes the theme's CSS-var values
```

The CSS:

```css
.theme-terracotta {
  --background: 60 9% 98%;       /* stone-50 */
  --foreground: 24 10% 10%;
  --border:     20 6% 90%;
  --primary:    21 90% 40%;      /* orange-700 */
  --primary-foreground: 0 0% 100%;
  --ring:       21 90% 48%;
  /* ... */
}

.theme-terracotta.dark {
  --background: 24 10% 5%;        /* stone-950 */
  --foreground: 60 9% 98%;
  --border:     12 6% 15%;
  --primary:    27 96% 61%;       /* orange-400 */
  --primary-foreground: 20 91% 14%;
  --ring:       27 96% 61%;
  /* ... */
}
```

A future theme is purely additive — define `.theme-rose { ... }` and `.theme-rose.dark { ... }` and add an entry to `themes/registry.ts`. No component code changes.

**Alternatives considered:**
- *Inline CSS-var setting via JavaScript:* Rejected. Forces JS to be the source of truth, breaks SSR, slow on theme switch.
- *Separate stylesheet per theme, swap `<link>` href:* Rejected. Network round-trip on switch, possible FOUC, harder for "System" mode to react to OS changes synchronously.

### Decision 4: First-paint via inline script in `index.html`

To avoid theme/mode FOUC (flash of wrong theme before React mounts), `index.html` carries a small inline `<script>` that runs synchronously before the React bundle:

```html
<script>
  (function () {
    var theme = localStorage.getItem('theme') || 'terracotta';
    var mode = localStorage.getItem('mode') || 'system';
    var isDark = mode === 'dark'
      || (mode === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches);
    document.documentElement.classList.add('theme-' + theme);
    if (isDark) document.documentElement.classList.add('dark');
  })();
</script>
```

The React `ThemeProvider` reads the same `localStorage` keys on mount, exposes them via context, and keeps the `<html>` classes in sync as the user changes their selection. The provider also registers a `prefers-color-scheme` `MediaQueryList` listener so System mode reacts to OS-level changes while the tab is open.

**Why inline script:** any other approach (React effect, postcss plugin, server-side render) flashes. The cost is ~15 lines of vanilla JS in `index.html`.

**Alternatives considered:**
- *Render-blocking render of `index.html` until React mounts:* The defaults Vite ships *do* render `index.html`; the unstyled HTML appears for a frame. This is the FOUC.
- *Skip system mode:* Rejected — system mode is a meaningful default for the "I never opened the picker" case.

### Decision 5: Theme registry as the future picker's source of truth

```ts
// src/themes/registry.ts
export type ThemeId = 'terracotta';  // future: | 'rose' | 'forest' | 'twilight'

export const themes: Record<ThemeId, { displayName: string; swatch: string }> = {
  terracotta: { displayName: 'Earth', swatch: '#c2410c' },
};

export const DEFAULT_THEME: ThemeId = 'terracotta';
```

`ThemeContext` reads from this registry to know which class to apply. The future theme picker reads the same registry to render its swatches. Adding a new theme is: add a CSS block in `index.css`, add an entry here, done.

The `displayName` is the user-visible label ("Earth" rather than "Terracotta"). The internal `id` ("terracotta") is the kebab-case identifier that shows up in `localStorage` and the `theme-*` class name.

### Decision 6: App shell owns auth, theming, and routing — pages slot in

Every authenticated route renders inside `<AppShell>`. The shell:
- Renders the persistent top nav (wordmark left; mode toggle + user menu right)
- Reads `useAuth()` and supplies the user's display name / initials to the avatar
- Owns the `ThemeProvider` boundary (above this is `<html>` classes; below this is provider context)
- Renders an `<Outlet />` from React Router for the page content
- Carries a `<Toaster />` at the root so any page can fire toasts

Unauthenticated routes (login, register, forgot/reset password, join via link) render *outside* the shell, on a centered card layout.

```
<App>
  ├─ <ThemeProvider>
  │   ├─ <AuthProvider>
  │   │   ├─ Routes
  │   │   │   ├─ <Route element={<AppShell />}>
  │   │   │   │   ├─ /                  ← HomePage
  │   │   │   │   ├─ /groups/:id        ← GroupDetailPage
  │   │   │   │   ├─ /wishlist/me       ← MyWishlistPage
  │   │   │   │   └─ /wishlist/:userId  ← MemberWishlistPage
  │   │   │   ├─ <Route element={<AuthLayout />}>
  │   │   │   │   ├─ /login
  │   │   │   │   ├─ /register
  │   │   │   │   ├─ /forgot-password
  │   │   │   │   ├─ /reset-password
  │   │   │   │   └─ /join/:code
```

This is the MFE-ready shape: today everything is a React component; tomorrow a `<Route element={<MicrofrontendShell name="wishlists" />} />` can replace any subtree without the shell knowing or caring.

### Decision 7: Typography scale and spacing rhythm

Notion-quiet wants restrained type and consistent rhythm.

**Type:**
- Base font: system stack (`ui-sans-serif, system-ui, ...`) — Tailwind's default `font-sans`. No web fonts in v1.
- Base size: 15px (Tailwind default is 16; we drop one notch for density).
- Scale: text-xs (12px), text-sm (13px), text-base (15px), text-lg (17px), text-xl (19px), text-2xl (23px), text-3xl (28px).
- Page H1: `text-2xl font-semibold tracking-tight` (≈ 23px). The current 56px h1 is far too loud.
- Section H2: `text-lg font-medium`. Body: `text-sm` for compact lists, `text-base` for prose.

**Spacing:**
- Tailwind's default 4px scale is fine. Use multiples of 4 (the `1` = 4px, `2` = 8px, `4` = 16px, `6` = 24px, `8` = 32px points on the scale).
- Page container: `max-w-2xl mx-auto px-4 sm:px-6 py-8 md:py-12`.
- Card padding: `p-4 sm:p-6`.
- Form field gap: `space-y-1.5` between label and input, `space-y-4` between fields.

**Radius:**
- shadcn's `--radius` token; we set it to `0.5rem` (8px). Buttons, inputs, cards, dialogs all key off this so the whole UI has a consistent corner radius.

### Decision 8: One accent color, used sparingly

Notion-quiet means the accent appears only on chrome:
- Primary `Button`
- Link text (`<a>` and `Link`)
- Selected list item
- Focus ring
- Active tab (none in v1, but reserved)

The accent never appears on:
- Surfaces / card backgrounds
- Page background
- Status badges (`Available` = muted; `Reserved by you` = `secondary` styling; `Reserved by someone` = `outline`; `Bought ✓` uses a small inline green ✓ glyph only — no accent-colored chip)
- Icon stroke color (icons inherit `currentColor` from their text context)

This is the discipline that separates "had a designer" from "had a theme."

### Decision 9: Replace `window.confirm` with `AlertDialog`

Every destructive action gets an `AlertDialog` with:
- Title that names the action and the target ("Delete 'Wool socks'?", "Leave 'Family'?")
- Description with the irreversible-ness made plain
- Cancel button (default variant)
- Confirm button (destructive variant)

There are two of these today (`MyWishlistPage` delete item, `GroupDetailPage` leave group). The pattern is identical and the dialog state can live as a controlled component on each page.

### Decision 10: Error display via `Alert` (form-level) and `Toast` (transient)

- `Alert variant="destructive"` for form-level errors above the affected fields (e.g. "Invalid email or password" on login, validation errors on item add/edit).
- `Toast` for transient errors that aren't tied to a form (e.g. "Failed to load wishlist" on a fetch failure). Toasts auto-dismiss; alerts stay until dismissed by user action or successful re-submit.

This replaces the red-`<p>`-tag pattern everywhere.

### Decision 11: Lucide icons; minimal set

Icons come from `lucide-react`. The set used in v1:
- `Gift` (wordmark glyph, replaces the favicon)
- `Sun` / `Moon` / `Monitor` (mode toggle)
- `LogOut` (user menu)
- `ExternalLink` (rendered next to each URL on wishlist items, optional — see Decision 12)
- `Trash2` (destructive button on item card, optional)
- `Plus` (Add buttons)
- `ChevronRight` (list row affordance)

Icons are sized 14–16px and inherit color from their text container.

### Decision 12: Wishlist item URLs render as small chip-shaped links

The previous PR introduced URL lists per wishlist item. The current render is a plain `<a>` per URL inline. For the modernized look:
- Each URL renders as a small outlined chip (`inline-flex items-center gap-1.5 rounded-md border px-2 py-1 text-xs`).
- Link text is the URL host with `www.` stripped (already what the previous PR does).
- An optional `ExternalLink` icon to the right at the same color as the text.
- Hover: subtle background fill matching `muted`.

This keeps the wishlist item card readable when many URLs are present.

## Risks / Trade-offs

- **[Risk]** Tailwind's purger sometimes strips classes that only appear inside template strings or conditionals if not configured carefully.
  **Mitigation:** Tailwind's content config explicitly globs all `src/**/*.{ts,tsx}` files. Where dynamic classes are unavoidable (e.g. a chip getting a color from a status enum), we use a `cva()` variants map so the generated class strings are statically analyzable.

- **[Risk]** shadcn primitives are copy-pasted into the repo, which means any future shadcn upstream improvement requires a manual re-copy.
  **Mitigation:** Accept the trade-off. shadcn's value is owning the code; the cost is on us. In practice these primitives change slowly.

- **[Risk]** The inline boot script in `index.html` runs untyped JavaScript and could silently desync from the React-side `ThemeContext` if their `localStorage` key names ever diverge.
  **Mitigation:** Both sides import a single `const STORAGE_KEY_THEME = 'theme'; const STORAGE_KEY_MODE = 'mode'` declaration from a shared constants file. The boot script can't import (it runs before bundling), so it hardcodes the same strings; the React side imports them. Include a code comment in `index.html` referencing the source of truth.

- **[Risk]** New dependencies (Tailwind, shadcn deps, Radix primitives) increase the install size and the bundle size.
  **Mitigation:** Tailwind's output is just CSS — typically 8–15 KB gzipped after purging. Radix primitives are tree-shakable, so only the imported pieces ship. Total expected bundle delta: ~50 KB gzipped, well within budget for an interior SPA.

- **[Risk]** A future theme that picks weird neutrals (e.g. "Cosmic" with deep blue surfaces) may make some status colors hard to distinguish without per-theme override of the `destructive` / status tokens.
  **Mitigation:** Each theme owns its full token sheet — destructive can shift hue per theme. Worth keeping in mind during theme additions but not a v1 concern.

- **[Trade-off]** Rebuilding every page on shadcn primitives is the largest visual diff this codebase will see in one PR. Review will require touching every page file. We accept this because the alternative — touching one page at a time — leaves the SPA in an inconsistent visual state for the duration of multiple PRs.

- **[Trade-off]** The boot script in `index.html` is unavoidably untyped vanilla JS. The cost is small (≈15 lines) and the value (no FOUC) is high.

## Migration Plan

The change is shipped as a single PR because the visual style is global. Rolling out half the pages on shadcn and half on inline styles would look broken.

Within the PR, work is structured to land in this order so each phase is independently testable:

1. **Tooling and tokens** — install Tailwind + shadcn, write the token sheet, set up the boot script, delete `App.css`, strip `index.css`. After this phase the app still renders (Tailwind is available, no components use it yet); the only visible change is whatever the stripped CSS no longer provides.
2. **Shell** — implement `ThemeProvider`, `AppShell`, `AuthLayout`, the routing rewrap. Pages still render with inline styles inside the new shell. After this phase the top nav appears and the mode toggle works.
3. **Primitives** — pull in the shadcn components.
4. **Page rebuild** — migrate pages one by one to the new primitives. Test each page works (forms submit, lists render, dialogs confirm) before the next.
5. **Cleanup pass** — `window.confirm` → `AlertDialog`, red-`<p>` → `Alert` / `Toast`, `err: any` → typed.
6. **Polish** — title, favicon, focus-ring sweep, mobile pass at 320px / 375px / 414px widths.

There is no rollback story besides "revert the PR." No data is touched. No API changes.

## Open Questions

_None._ Theme picker and DB persistence are explicitly deferred future work. The two-class-on-root architecture and the theme registry make those changes additive.
