## 1. Reservations

- [x] 1.1 `GiftClaimRepository.CreateAsync`: unique violation → `ConflictException` ("Someone else just reserved this gift.")
- [x] 1.2 Postgres test: four givers reserve at once, one wins and the rest get a conflict (fails without 1.1)

## 2. Duplication

- [x] 2.1 `ClaimsPrincipal.GetUserId()` replaces the copies in six controllers
- [x] 2.2 `api/errors.ts` (`apiErrorMessage`, `apiErrorStatus`) replaces the copies in the web app

## 3. Code splitting

- [x] 3.1 Every page through `lazyPage`; `Suspense` around the layouts' `Outlet`
- [x] 3.2 `lazyPage`: when a page's code can't load, update and reload once (not again within 10 s)
- [x] 3.3 Service worker: cache and prune every chunk the shell's scripts can load ("assets/x" and "./x.js" references)
- [x] 3.4 Browser: all pages open, all 13 page chunks are cached after the first visit; a missing chunk gives 404 from the server

## 4. Release

- [x] 4.1 Release note confirmed with the user: "Performance fixes"
- [x] 4.2 Tests (with Postgres), lint, build
