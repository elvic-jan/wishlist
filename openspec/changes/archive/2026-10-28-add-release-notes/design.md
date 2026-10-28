## Context

`lib/appUpdate.ts` detects a release by fetching `/index.html` (no-cache) and comparing its `/assets/index-<hash>.js` entry with the running one. The notice (`UpdateToaster`) has a fixed text. The page is served by the API (`SpaPage`), which only rewrites the og:/twitter: meta tags.

## Goals / Non-Goals

**Goals:** the notice says what the deployed version brings, with one obvious place to write that text per change.

**Non-Goals:** a changelog page, several notes per version, translations.

## Decisions

**1. The note lives in `index.html` as a meta tag.**
`<meta name="release-note" content="…">`. The update check already downloads the new page, so reading the note costs no extra request, and the note always belongs to exactly the version that's deployed. *Alternatives:* an API endpoint or a JSON file. Either needs another request and another file to keep in step with the build. A build-time env var hides the text in CI settings, away from the change that it describes.

**2. Parse with `DOMParser`, fall back to the generic text.**
`checkForUpdate` keeps the note it read alongside `available`, trimmed and capped at 200 characters. A missing or empty note gives `null`, and the toast then shows "Refresh to get the latest GiftCircles." When several releases happened between visits, the fetched page is the newest one, so its note is the one shown.

**3. A test guards the rule.**
An API test reads the repo's `index.html` and fails when the release note is missing or empty. A second test checks that `SpaPage.Render` leaves the tag untouched. Keeping the text up to date per change is a workflow rule: it's listed in each change's tasks and confirmed with the user before the PR.

## Risks / Trade-offs

- [A change forgets to update the note, so the previous note shows again] → the per-change task and the user's confirmation; the old note is at worst out of date, never wrong for the version it shipped with.
- [Archive-only or docs PRs] → they don't change the built app files, so the page and its entry hash stay the same and no notice is shown.
