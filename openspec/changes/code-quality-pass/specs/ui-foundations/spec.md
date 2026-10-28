## ADDED Requirements

### Requirement: Pages load on demand
The SPA SHALL load each page's code the first time that page is opened, not with the app's first download. The service worker SHALL cache the code of every page of the current version ahead of time, as it does for the app shell, so pages open without a network round trip on later visits and keep working after the previous version's files are gone from the server. If a page's code can't be loaded (for example in a tab opened before a release), the SPA SHALL load the new version once instead of showing a broken page.

#### Scenario: First visit downloads less
- **WHEN** someone opens the app for the first time
- **THEN** the first download holds the app frame and shared code, and each page's own code arrives when that page is opened

#### Scenario: Pages are cached ahead
- **WHEN** the service worker caches a version of the app
- **THEN** it also caches every page's code that version can load later, and removes page code no kept version needs

#### Scenario: Old tab after a release
- **WHEN** a tab running the previous version opens a page whose code is no longer on the server or in the cache
- **THEN** the SPA loads the new version once, and opens the page there
