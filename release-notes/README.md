# Release notes

Each pull request that changes the app adds one file here describing it for
the people who use Kadoozle. The deploy turns the files into a version number
and the list on the What's new page (`/whats-new`), and the GitHub Release.

```
---
type: feature
---
You can now sort your wishlist
```

- **File name**: anything ending in `.md`, e.g. the branch name. One file per
  pull request, so pull requests never conflict over notes.
- **type**: `feature` (new version 1.1.0 → 1.2.0), `fix` (1.1.0 → 1.1.1) or
  `major` (2.0.0; only by hand, for big changes).
- **Text**: one note per line, at most 200 characters, written for users.
  The repository is public and the notes show in the app and on GitHub, so
  nothing internal goes in them.
- **No file** (a Dependabot update, say): the release still gets a patch
  version, listed as "Small fixes and improvements".
- Files stay here after release; the deploy works out which ones are new from
  the git tags (`scripts/release-version.mjs`). Don't edit or delete old ones.
