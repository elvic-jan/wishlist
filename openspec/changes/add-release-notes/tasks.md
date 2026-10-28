## 1. Web

- [x] 1.1 `index.html`: `<meta name="release-note" content="…">` with a comment saying every change sets it
- [x] 1.2 `appUpdate.ts`: read the note from the fetched page (trimmed, max 200 characters, empty → none); `useUpdateNote()`
- [x] 1.3 `UpdateToaster`: show the note, or "Refresh to get the latest GiftCircles." without one

## 2. Tests

- [x] 2.1 `index.html` has a non-empty release note; serving the page keeps it

## 3. Release note

- [x] 3.1 Release note for this change confirmed with the user: "Updates now tell you what's new"

## 4. Check

- [x] 4.1 Browser: the notice shows the note of a newer build, and the generic text without one
