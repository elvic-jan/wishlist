## 1. Refresh tokens

- [x] 1.1 `RefreshToken.Token` → `TokenHash`; index on `UserId`; migration `HashRefreshTokens` renames the column and hashes existing values in SQL
- [x] 1.2 `TokenService`: hash on create and lookup; `ConsumeRefreshTokenAsync` (one conditional update); cleanup of expired and long-revoked rows on create
- [x] 1.3 `RefreshTokenUseCase` uses `ConsumeRefreshTokenAsync`; test fakes follow
- [x] 1.4 SPA: refresh inside a Web Lock across tabs, one retry before going to the login page

## 2. Sign-in timing

- [x] 2.1 `UserRepository.CheckPasswordAsync` verifies against a dummy hash when there's no user; `LoginUserUseCase` calls it for unknown emails

## 3. Input limits

- [x] 3.1 `InputLimits` (display name 100, email 256, password 256, circle name 200, title 300, description 5000, URL 2048)
- [x] 3.2 Checks in `RegistrationGate`/register, passkey sign-up, profile update (new `UpdateProfileUseCase`, also refuses empty), circle creation, item add/edit, URL validator
- [x] 3.3 SPA inputs get matching `maxLength`

## 4. Configuration

- [x] 4.1 Startup refuses a missing, placeholder or short `Jwt:Key` outside Development
- [x] 4.2 Invite links from `App:PublicBaseUrl` (`GenerateGroupInvitationUseCase` takes `IAppUrlProvider`)

## 5. Tests

- [x] 5.1 API tests: limits (400 not 500), forged Host, JWT key check, unknown-email sign-in still 401
- [x] 5.2 Postgres tests: stored value is a hash, old plain token survives the migration, concurrent refresh gives one winner, cleanup

## 6. Release

- [ ] 6.1 Release note confirmed with the user: "Security fixes"
- [x] 6.2 Tests (with Postgres), lint, build
