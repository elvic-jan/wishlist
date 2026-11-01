## 1. Component

- [x] 1.1 `components/ui/password-input.tsx`: `Input` with an eye button inside the right edge (Eye/EyeOff, `type="button"`, aria-label + aria-pressed, keeps focus in the field), hidden by default
- [x] 1.2 `index.css`: hide `input::-ms-reveal`

## 2. Pages

- [x] 2.1 `LoginPage.tsx`: password field uses `PasswordInput`
- [x] 2.2 `RegisterPage.tsx`: password field uses `PasswordInput`
- [x] 2.3 `RecoverPage.tsx`: new and confirm fields each use `PasswordInput`

## 3. Release

- [x] 3.1 Release note in `index.html`: "Option to unmask password" (confirmed with the user)
- [x] 3.2 SPA lint and build pass
- [x] 3.3 Browser check (built SPA, no API): Login shows/hides, keeps focus, doesn't submit; Recover fields toggle separately. Register needs a valid invite from the API, so it was not opened; it uses the same component
