## 1. Deploy

- [x] 1.1 `scripts/release-note-guard.sh`: compare with the live note (`release-note` or `release-note-previous`); rename the tag when unchanged; keep the note if the live site can't be read
- [x] 1.2 `deploy.yml`: run it before the image build
- [x] 1.3 Tried locally against four fake live pages (same, different, previous-same, previous-different) and an unreachable one

## 2. Pull requests

- [x] 2.1 `ci.yml`: `Release note` job warns when `src/` changes but the note doesn't

## 3. Rollout

- [x] 3.1 First deploy after merging: the deploy log shows the guard's notice (#64: "unchanged since the live version (Performance fixes)")
