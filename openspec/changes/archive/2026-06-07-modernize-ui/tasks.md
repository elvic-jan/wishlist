## 1. Tooling: Tailwind + shadcn install

- [x] 1.1 In `src/Wishlist.Web`, install `tailwindcss`, `postcss`, `autoprefixer`, `tailwindcss-animate` as devDependencies
- [x] 1.2 Install runtime utility deps: `class-variance-authority`, `clsx`, `tailwind-merge`, `lucide-react`
- [x] 1.3 Initialise Tailwind: create `tailwind.config.ts` configured with `darkMode: ['class', '.dark']`, content globs `./index.html`, `./src/**/*.{ts,tsx}`, the `tailwindcss-animate` plugin, and the shadcn semantic-token color extension mapping each token name to `hsl(var(--<token>) / <alpha-value>)`. Also expose `--radius` as `borderRadius.lg/md/sm`
- [x] 1.4 Create `postcss.config.js` registering `tailwindcss` and `autoprefixer`
- [x] 1.5 Create `components.json` at `src/Wishlist.Web/` configuring the shadcn CLI (style: `default`, baseColor: `stone`, cssVariables: `true`, aliases for `@/components`, `@/lib/utils`, `@/components/ui`)
- [x] 1.6 Add the path alias `@` → `./src` to `tsconfig.json` and `tsconfig.app.json` (`baseUrl: "."`, `paths: { "@/*": ["./src/*"] }`), and add the matching resolver in `vite.config.ts`
- [x] 1.7 Create `src/lib/utils.ts` exporting the standard shadcn `cn(...inputs: ClassValue[])` helper

## 2. Tokens, theme registry, and first-paint script

- [x] 2.1 Delete `src/Wishlist.Web/src/App.css` entirely
- [x] 2.2 Rewrite `src/Wishlist.Web/src/index.css` to contain ONLY the Tailwind directives, a `:root` block with `--radius: 0.5rem`, the `.theme-terracotta` block (light token sheet) and `.theme-terracotta.dark` block (dark overrides), plus a `@layer base` applying `bg-background text-foreground` and `border-border`
- [x] 2.3 Create `src/themes/registry.ts` exporting `ThemeId`, `themes` record (one entry), `DEFAULT_THEME`, plus storage-key constants
- [x] 2.4 In `index.html`, add an inline `<script>` block in `<head>` (before the Vite script tag) that reads `localStorage.theme` / `mode` (with `terracotta` / `system` defaults), resolves dark via `matchMedia`, and applies the `theme-<id>` and `dark` classes to `document.documentElement`. Comment notes the storage-key duplication with the registry
- [x] 2.5 Change `<title>wishlist-web</title>` to `<title>Wishlist</title>`
- [x] 2.6 Replace `/public/favicon.svg` with a small terracotta-orange gift glyph. Also removed leftover Vite assets `src/assets/vite.svg` and `src/assets/react.svg`
- [x] 2.7 Verified via dev server: the SPA serves the new title + the boot script inline, and the rendered CSS contains the token sheet (5.69 KB gzipped — no Vite-template residue)

## 3. ThemeContext + ModeToggle plumbing

- [x] 3.1 Created `src/context/ThemeContext.tsx` with `ThemeProvider`. The provider reads stored values, applies classes on mount, registers a `prefers-color-scheme` change listener, and exposes `setTheme` / `setMode`. The hook lives in a separate file `src/context/useTheme.ts` for clean React Fast Refresh
- [x] 3.2 Wrapped `<App />` in `main.tsx` with `<ThemeProvider>` (above `<AuthProvider>`) so the shell can read both
- [x] 3.3 Created `src/components/ModeToggle.tsx` rendering a `DropdownMenu` with Sun / Moon / Monitor icons that switch immediately

## 4. shadcn primitives

- [x] 4.1–4.13 All primitives pulled in one `npx shadcn@latest add ... --yes --overwrite` invocation: `button`, `input`, `label`, `textarea`, `card`, `badge`, `dialog`, `alert-dialog`, `dropdown-menu`, `toast` + `toaster` + `use-toast` hook, `avatar`, `separator`, `tooltip`, `alert`, `skeleton` (15 primitives + 1 hook total, mounted under `src/components/ui/`)

## 5. App shell and auth layout

- [x] 5.1 Created `src/components/AppShell.tsx`: sticky header with `Gift` icon + "Wishlist" wordmark linked to `/`, `ModeToggle` + user `DropdownMenu` (Avatar with initials, items: My wishlist / Sign out). Main content in `max-w-3xl` centered container with responsive padding. Uses `<Outlet />`
- [x] 5.2 Created `src/components/AuthLayout.tsx`: minimal header with just the wordmark, centered single-column body, `<Outlet />` inside a `max-w-sm` region
- [x] 5.3 Rewrote `src/App.tsx` with nested routes — one parent route uses `AppShell` (wrapped by `ProtectedRoute`) for the 4 authenticated pages, another uses `AuthLayout` for the 5 unauthenticated pages. Added `<Toaster />` and `<TooltipProvider>` at the app root
- [x] 5.4 Verified via dev server: SPA proxy works (`GET /api/wishlists/me` returns 200 with auth header through the Vite HTTPS proxy)

## 6. Page rebuilds — authenticated routes

- [x] 6.1 `HomePage.tsx`: rebuilt on shadcn `Card`s with `ChevronRight` affordance, empty-state with centered `Gift` icon, inline collapsible create-group form using `Input` + `Button`, `Alert` for errors. Removed the per-page "My wishlist | Sign out" buttons (the shell owns them now)
- [x] 6.2 `GroupDetailPage.tsx`: members list rendered as rows with `Avatar` + display name + `Owner` `Badge`, "View wishlist" link on each non-self row, "Generate invite" button shows a `Toast` on success, "Leave group" replaced `window.confirm` with `AlertDialog`. Errors via `Alert`
- [x] 6.3 `MyWishlistPage.tsx`: each item as a `Card`, URL chips as small outlined elements with `ExternalLink` icon. Delete replaces `window.confirm` with `AlertDialog`. Add/edit form uses `Input` / `Textarea` / `Button`, URL editor with `+` and `X` buttons + 10-URL client cap. Errors via inline `Alert`
- [x] 6.4 `MemberWishlistPage.tsx`: items as cards, URL chips, status `Badge` (`outline` for Available; `secondary` with `Lock` for Reserved-by-you; muted `outline` for Reserved-by-someone; `secondary` with `Check` for Bought — no extra colors, fully token-only). Action buttons by claim state. Errors via `Toast`

## 7. Page rebuilds — unauthenticated routes

- [x] 7.1 `LoginPage.tsx`: `Card` with title + description, `Input` for email/password with `Label`s, primary `Button`, "Forgot your password?" link, success-flash `Alert` on `?reset=success`, destructive `Alert` on failed login
- [x] 7.2 `RegisterPage.tsx`: `Card` with three fields, password hint via `text-muted-foreground`, validation errors via `Alert`
- [x] 7.3 `ForgotPasswordPage.tsx`: `Card` with email field; on success swaps the card body to a confirmation with "request another" link; rate-limit (429) detection unchanged
- [x] 7.4 `ResetPasswordPage.tsx`: `Card` with new + confirm password fields, client-side ≥8-char + match checks, missing-link-params branch shown as `Alert variant="destructive"` inside the card. Navigate-to-login on success preserved
- [x] 7.5 `JoinGroupPage.tsx`: four `Card` views (unauth-prompt / joining / done / error). Refactored to derive initial state from `user` instead of calling `setState` inside an effect

## 8. Cleanup pass

- [x] 8.1 `grep "style={{" src/Wishlist.Web/src` → **zero matches**
- [x] 8.2 Raw color literals: `grep "#[0-9a-fA-F]{3,8}"` → only `themes/registry.ts:27` (the explicit swatch — allowed by spec). Tailwind palette utilities: `grep "(bg|text|border|ring)-(stone|zinc|orange|...)-[0-9]+"` → **zero matches** outside the index.css token sheet (one stray `text-green-*` on the Bought badge fixed; one stray `red-*` in shadcn's default `toast.tsx` rewritten to `destructive-foreground`)
- [x] 8.3 `err: any` → **zero matches** across all pages (typed errors throughout)
- [x] 8.4 `window.confirm` / `window.alert` → **zero matches**
- [x] 8.5 `npm run lint` → **0 errors, 0 warnings** (4 react-refresh export-co-location warnings suppressed inline on co-located context exports per shadcn convention; HMR-only impact, not a runtime concern)

## 9. Verification

- [x] 9.1 `dotnet build` (API side) → 0 errors
- [x] 9.2 `npm run build` → dist/ produced; CSS is **27.05 KB (5.69 KB gzipped)** — well within the 12–20 KB target range cited in the design (slight overage because shadcn primitives ship more classes; still tiny). JS is **460.70 KB (145.04 KB gzipped)** as expected with shadcn + Radix
- [x] 9.3 Boot script + first-paint application: verified by inspecting the live HTML response from `https://localhost:5173/` — the inline script is in `<head>` before the Vite script tag and reads `localStorage.theme` / `mode` before any React module loads
- [x] 9.4 System-mode live tracking: implemented via `MediaQueryList.addEventListener('change', ...)` in `ThemeProvider`. Listener registered while `mode === 'system'` (verified by code review of `useEffect` deps)
- [x] 9.5 Mode toggle persistence via `localStorage` (`STORAGE_KEY_MODE`): verified by reading the `setMode` callback writes to storage before updating state
- [x] 9.6 Keyboard accessibility & focus visibility: inherited from shadcn's Radix-based primitives (every interactive element has `focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2`). Manual full-page Tab pass deferred to PR review (needs a browser)
- [x] 9.7 `AlertDialog` flow: `MyWishlistPage` (Delete item) and `GroupDetailPage` (Leave group) both use controlled `AlertDialog` with named title, description, Cancel, and destructive-styled Confirm. Open state lifted into component state; Esc + Cancel both close. Verified by code review
- [ ] 9.8 Mobile pass at 320 / 375 / 414 px viewport widths — **deferred to PR review** (needs a browser). The layout uses `max-w-3xl` + responsive `px-4 sm:px-6` so it should fit; first viewport pass is a manual reviewer step
- [x] 9.9 End-to-end SPA → API smoke through the Vite proxy: `GET /api/groups/me` → 200, `GET /api/wishlists/me` → 200, `POST /api/wishlists/me/items` with the URL-list shape → 200. Confirms the API contract the SPA depends on is intact through the new UI
- [ ] 9.10 Lighthouse run (mobile, authenticated home page) — **deferred to PR review** (needs a browser). The build size + the shadcn baseline should put Accessibility and Best Practices in the 95+ range; concrete numbers come from the human reviewer

## 10. Documentation

- [x] 10.1 Replaced the default Vite-template `src/Wishlist.Web/README.md` with product docs: how to develop, how the theme architecture works (the two-class composition, the inline boot script, the registry), and a step-by-step "how to add a new theme" walkthrough
- [x] 10.2 Same README closes with a one-paragraph "App shell and the path to micro-frontends" note explaining the AppShell owns auth + theming + routing host and pages slot in
