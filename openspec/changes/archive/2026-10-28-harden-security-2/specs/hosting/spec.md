## ADDED Requirements

### Requirement: Startup refuses a weak JWT signing key
Outside the Development environment the system SHALL refuse to start when `Jwt:Key` is missing, equals the placeholder value from `appsettings.json`, or is shorter than 32 bytes, so tokens are never signed with a key that is public or guessable.

#### Scenario: Placeholder key in production
- **WHEN** the app starts in `Production` with `Jwt:Key` still set to the placeholder from `appsettings.json`
- **THEN** startup fails with a configuration error and the platform does not route traffic to that revision

#### Scenario: Short key in production
- **WHEN** the app starts in `Production` with a `Jwt:Key` shorter than 32 bytes
- **THEN** startup fails with a configuration error

#### Scenario: Development keeps working
- **WHEN** the app starts in `Development` with the placeholder key
- **THEN** it starts normally
