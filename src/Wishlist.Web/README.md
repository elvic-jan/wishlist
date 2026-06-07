# Wishlist Web

The React + Vite SPA for the wishlist app. Built with Tailwind CSS and
shadcn/ui primitives.

## Development

```bash
# From src/Wishlist.Web
npm install
npm run dev    # serves at https://localhost:5173
```

The dev server uses `@vitejs/plugin-basic-ssl` to serve HTTPS so the
backend's `Secure` refresh-token cookie is accepted by the browser. The
self-signed cert prompts a one-time browser warning on first visit.

`npm run build` produces `dist/`; `npm run lint` runs ESLint with zero
acceptable warnings.

The backend (`Wishlist.API`) needs to be running on `https://localhost:7277`
(its default `https` launch profile). The SPA proxies `/api/*` calls there
via `vite.config.ts`.

## Theming

The app ships one theme today — **Terracotta** (orange-on-stone) — with
both light and dark variants. The architecture is built so user-selectable
themes can be added later as a purely additive change.

### How a theme is applied

The `<html>` element carries two classes:

```
<html class="theme-terracotta dark">
       │                    │
       │                    └─ activates the dark variant of the active theme
       └─ scopes the theme's CSS-variable values
```

A tiny inline script in `index.html` reads `localStorage.theme` and
`localStorage.mode` and applies the two classes **before** the React bundle
mounts. This avoids any flash of the wrong theme on first paint.

After mount, `ThemeProvider` (`src/context/ThemeContext.tsx`) keeps the
classes in sync as the user toggles their preference, and registers a
`prefers-color-scheme` listener so "System" mode reacts to OS-level theme
changes live without a reload.

### How to add a new theme

1. Define the theme's token sheet in `src/index.css`:

   ```css
   .theme-bloom {
     --background: 0 0% 100%;
     --foreground: 351 30% 10%;
     --primary: 350 89% 60%;     /* rose-500 */
     /* ...all the other tokens, see existing themes */
   }

   .theme-bloom.dark {
     --background: 351 30% 5%;
     /* ...dark overrides */
   }
   ```

2. Add an entry to `src/themes/registry.ts`:

   ```ts
   export type ThemeId = 'terracotta' | 'bloom'

   export const themes: Record<ThemeId, ThemeMeta> = {
     terracotta: { displayName: 'Earth', swatch: '#c2410c' },
     bloom:      { displayName: 'Bloom', swatch: '#e11d48' },
   }
   ```

No other application code needs to change. The future theme picker UI
will read from this registry and render one option per entry.

### Token discipline

Component code reads **only** the semantic tokens (`bg-background`,
`text-primary`, `border-border`, etc.) and never raw Tailwind palette
classes (`bg-orange-700`, `text-stone-500`). That discipline is what makes
the theme additions above a one-file change. The token-name list is the
contract; the `tailwind.config.ts` exposes them all as `hsl(var(--<token>))`.

## App shell and the path to micro-frontends

Every authenticated route renders inside `<AppShell />`
(`src/components/AppShell.tsx`), which is the only thing that knows about
auth, theming, and the persistent top nav. Pages slot into the shell's
`<Outlet />` and don't carry their own headers.

This shape is set up so future micro-frontend extraction is incremental:
the shell becomes the host (it already owns the cross-cutting concerns),
and individual subtrees can be lazy-loaded or federated without the shell
caring how they were produced. CSS tokens are exposed as standard CSS
variables on `<html>`, so an MFE can consume them by name without needing
to import a runtime.
