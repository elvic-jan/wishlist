# Release notes

Each pull request that changes the app adds one file here describing it for
the people who use Kadoozle. The deploy turns the files into a version number
and the list on the What's new page (`/whats-new`), and the GitHub Release.

```
---
type: feature
---
You can now sort your wishlist
nl: Je kan je verlanglijst nu sorteren
```

- **File name**: anything ending in `.md`, e.g. the branch name. One file per
  pull request, so pull requests never conflict over notes.
- **type**: `feature` (new version 1.1.0 → 1.2.0), `fix` (1.1.0 → 1.1.1) or
  `major` (2.0.0; only by hand, for big changes).
- **Text**: one note per line, at most 200 characters, written for users,
  in English. The repository is public and the notes show in the app and on
  GitHub, so nothing internal goes in them.
- **Translation**: right under each note, the same note in Dutch on a line
  starting with `nl:` (also at most 200 characters). The What's new page shows
  the note in the reader's language; the GitHub Release uses the English one.
  The checks fail when a note has no `nl:` line.
- **No file** (a Dependabot update, say): the release still gets a patch
  version, listed as "Small fixes and improvements".
- Files stay here after release; the deploy works out which ones are new from
  the git tags (`scripts/release-version.mjs`). Don't delete or rename old ones; edit one
  only to fix its wording or a translation (the What's new page then shows the
  fixed text for that release too).
