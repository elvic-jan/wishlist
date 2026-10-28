## Why

The release note lives in `index.html` and stays there until a change replaces it. A release that doesn't touch it, such as a Dependabot update, would show the previous release's note again ("Performance fixes" for a package bump).

## What Changes

- **Deploy:** before the image is built, `scripts/release-note-guard.sh` compares the note with the one on the live site.
  - If they're the same, it renames the tag to `release-note-previous`. The app ignores that tag and shows "Refresh to get the latest GiftCircles." The next deploy still sees which note was last written.
  - If the live site can't be read, the note is kept.
  - The live address comes from the `PUBLIC_SITE_URL` variable, or `https://www.giftcircles.app` by default.
- **Pull requests:** a new `Release note` check warns, without failing, when a PR changes app code (`src/`) but not the note. It shows the note when there is one.
- No app code changes.

## Capabilities

### Modified Capabilities
- `ui-foundations`: a release note shows only with the release it was written for.

## Impact

- `scripts/release-note-guard.sh` (new), `deploy.yml` (one step before the image build), `ci.yml` (`release-note` job).
- Known limit: two releases in a row with the same text. The second one shows the generic text, so write e.g. "More security fixes".
