## 1. Hide passkeys on Firefox for Android

- [x] 1.1 `webauthn.ts`: `isFirefoxAndroid()` from the user agent; `passkeysSupported()` returns false there
- [x] 1.2 `SecurityPage.tsx`: on Firefox for Android, explain that fingerprint sign-in doesn't work there instead of the generic "can't create passkeys" text

## 2. Release and checks

- [x] 2.1 Release note in `index.html`: "In Firefox on Android, sign in with your password" (confirmed with the user)
- [x] 2.2 SPA lint and build pass
- [x] 2.3 Check of the Firefox-for-Android detection against sample user agents (phone, tablet hidden; Chrome, Samsung Internet, Safari, Firefox on Windows, Mac and iPhone shown). The browser pane could not open the local dev server (untrusted certificate)
