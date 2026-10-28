## 1. Update checks

- [x] 1.1 `checkForUpdate` records whether the server answered; 45 s timeout per check
- [x] 1.2 `startUpdateChecks`: retry failed checks (10 s, 30 s, 1 min, then 5 min), every 15 min otherwise, while visible; also on `online` and on `pageshow` from the page cache
- [x] 1.3 Browser: a failed first check is retried after ~10 s and shows the notice; the notice shows on load when a newer version is live

## 2. Release

- [ ] 2.1 Release note confirmed with the user: "More reliable update notices"
- [x] 2.2 Lint, build, tests
