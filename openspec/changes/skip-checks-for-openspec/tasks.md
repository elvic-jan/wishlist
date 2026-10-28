## 1. Checks

- [x] 1.1 `ci.yml`: `changes` job (fetch-depth 2, diff `HEAD^1..HEAD` without `openspec/`); `api` and `web` need it and run only when other files changed
- [ ] 1.2 This PR (workflow change) runs all checks; the next OpenSpec-only PR (this change's archive) skips them and can be merged
