## Decisions

**Compare with the live site, not with git history.** The live page is what people's notices are based on, and the build job can read it without credentials. Git history would need to know which commit was last deployed. Deploys can be cancelled or rolled back, and the live page reflects that automatically.

**Rename, don't delete.** If an unchanged note were removed, the next release would compare against "no note", see its own (still unchanged) note as new, and show it again. `release-note-previous` keeps the last written note on the live page, so the comparison stays right release after release.

**Warn, don't fail, on pull requests.** Dependabot PRs change the app without a note by design. The deploy handles them, and a hard failure would block them.
