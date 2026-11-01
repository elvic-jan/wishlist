## MODIFIED Requirements

### Requirement: Sign-in redirect stays on the site
After signing in, the SPA SHALL follow the login page's `redirect` parameter only when it is a path on the same site. A value with a backslash, a protocol-relative `//` start, a scheme, or anything else that resolves to another origin SHALL be ignored, and the SPA SHALL go to the Wishlists page instead. Without a `redirect` parameter the SPA SHALL also go to the Wishlists page.

#### Scenario: Redirect within the site
- **WHEN** a user signs in from `/login?redirect=/circles/123`
- **THEN** the SPA opens `/circles/123`

#### Scenario: No redirect
- **WHEN** a user signs in from `/login`
- **THEN** the SPA opens `/wishlists`

#### Scenario: Redirect to another site is ignored
- **WHEN** a user signs in from `/login?redirect=/\evil.example` (or `//evil.example`, or `https://evil.example`)
- **THEN** the SPA opens `/wishlists` and never leaves the site
