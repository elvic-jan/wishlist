# wishlist
website for maintaining wishlists

## Deployment

Production runs on Azure Container Apps with PostgreSQL Flexible Server, deployed from
`main` by GitHub Actions after a manual approval. Setup, operations and costs are
described in [infra/README.md](infra/README.md).

## Languages

The app is offered in English and Dutch. Texts are written in English and
translated in one place per side:

- **Web**: `src/Wishlist.Web/src/lib/i18n`. `en.ts` holds every text under a
  key; `nl.ts` must have the same keys (the build fails otherwise). Use
  `useT()` / `<Trans>` in components; see the comment at the top of
  `index.tsx`.
- **Server** (error messages, emails, lasting-toast messages, the invite link
  preview): written in English in the code and translated in
  `src/Wishlist.Application/Localization/DutchTexts.cs`, keyed by the English
  text. `TranslationTests` fails when a text has no Dutch entry.
- **Release notes**: each note has an `nl:` line; see
  [release-notes/README.md](release-notes/README.md).

What people type themselves (gift titles and descriptions, names, circle
names) is never translated, and is marked `translate="no"` so the browser's
page translation leaves it alone too.

A person's language follows their browser until they choose one (account menu,
or under the sign-in pages); the choice is saved on the account and used for
their emails.
