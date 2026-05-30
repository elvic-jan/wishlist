## 1. Project Setup

- [x] 1.1 Create C# ASP.NET Core solution with projects: API (BFF), Application, Domain, Infrastructure
- [x] 1.2 Create React + TypeScript frontend project (Vite) inside the solution
- [x] 1.3 Add NuGet packages: EF Core, ASP.NET Core Identity, JWT bearer authentication, PostgreSQL driver
- [x] 1.4 Add npm packages: React Router, Axios (or fetch wrapper), a UI component library
- [x] 1.5 Configure the BFF to serve the React SPA static files in production
- [x] 1.6 Set up Docker Compose with PostgreSQL for local development

## 2. Domain Layer

- [x] 2.1 Create entity classes: User, Group, GroupMember, GroupInvitation, WishlistItem, GiftClaim
- [x] 2.2 Define GiftClaimStatus enum (Reserved, Bought)
- [x] 2.3 Define GroupMemberRole enum (Owner, Member)
- [x] 2.4 Add domain rule: WishlistItem.GetClaimsVisibleTo(requestingUserId) — returns null if requester is the owner
- [x] 2.5 Add domain rule: GiftClaim can only be held by one user at a time (enforced via domain validation)

## 3. Infrastructure Layer

- [x] 3.1 Create EF Core DbContext with DbSets for all entities
- [x] 3.2 Configure entity relationships and constraints (unique index on GiftClaims.WishlistItemId)
- [x] 3.3 Write and apply initial database migration
- [x] 3.4 Implement IUserRepository, IGroupRepository, IWishlistRepository, IGiftClaimRepository
- [x] 3.5 Configure ASP.NET Core Identity with the User entity
- [x] 3.6 Implement JWT token service (issue access tokens, manage refresh tokens in DB)

## 4. Application Layer — Use Cases

- [x] 4.1 RegisterUser use case with email uniqueness check
- [x] 4.2 LoginUser use case returning access + refresh tokens
- [x] 4.3 RefreshToken use case with token rotation
- [x] 4.4 LogoutUser use case revoking the refresh token
- [x] 4.5 CreateGroup use case (creator becomes owner member)
- [x] 4.6 GenerateGroupInvitation use case (7-day expiry, member-only access)
- [x] 4.7 JoinGroupViaInvitation use case (validates expiry, handles already-member case)
- [x] 4.8 ListGroupMembers use case (member-only access)
- [x] 4.9 LeaveGroup use case (blocks owner leaving while others remain)
- [x] 4.10 AddWishlistItem use case
- [x] 4.11 EditWishlistItem use case (owner-only)
- [x] 4.12 DeleteWishlistItem use case (cascades to GiftClaim)
- [x] 4.13 GetOwnWishlist use case (strips claim data from response)
- [x] 4.14 GetMemberWishlist use case (verifies shared group membership, annotates claim status)
- [x] 4.15 ListGroupWishlists use case (returns other members' wishlists, excludes requester's own)
- [x] 4.16 ReserveGift use case (validates unclaimed, blocks owner self-claim, blocks non-member)
- [x] 4.17 MarkGiftAsBought use case (claimer-only, Reserved → Bought)
- [x] 4.18 ReleaseGiftClaim use case (claimer-only, removes claim record)

## 5. BFF API Controllers

- [x] 5.1 POST /api/auth/register — register a new user
- [x] 5.2 POST /api/auth/login — login and return tokens
- [x] 5.3 POST /api/auth/refresh — refresh access token
- [x] 5.4 POST /api/auth/logout — revoke session
- [x] 5.5 GET /api/auth/me — return current user's profile
- [x] 5.6 PATCH /api/auth/me — update display name
- [x] 5.7 POST /api/groups — create a group
- [x] 5.8 POST /api/groups/{groupId}/invitations — generate invitation link
- [x] 5.9 POST /api/invitations/{code}/join — join via invitation code
- [x] 5.10 GET /api/groups/{groupId}/members — list group members
- [x] 5.11 DELETE /api/groups/{groupId}/members/me — leave group
- [x] 5.12 GET /api/groups/{groupId}/wishlists — list all member wishlists in a group
- [x] 5.13 GET /api/wishlists/me — get own wishlist (no claim data)
- [x] 5.14 POST /api/wishlists/me/items — add wishlist item
- [x] 5.15 PATCH /api/wishlists/me/items/{itemId} — edit wishlist item
- [x] 5.16 DELETE /api/wishlists/me/items/{itemId} — delete wishlist item
- [x] 5.17 GET /api/wishlists/{userId} — get another member's wishlist with claim status
- [x] 5.18 POST /api/wishlists/{userId}/items/{itemId}/claim — reserve a gift
- [x] 5.19 PATCH /api/wishlists/{userId}/items/{itemId}/claim — mark gift as bought
- [x] 5.20 DELETE /api/wishlists/{userId}/items/{itemId}/claim — release a claim
- [x] 5.21 Add global error-handling middleware mapping domain exceptions to HTTP status codes

## 6. React Frontend — Setup and Routing

- [x] 6.1 Configure React Router with routes: login, register, home (group list), group detail, wishlist view, own wishlist
- [x] 6.2 Create Axios instance with JWT interceptor (attaches access token to requests)
- [x] 6.3 Implement silent token refresh on 401 responses using the refresh token cookie
- [x] 6.4 Create AuthContext providing current user state and login/logout actions

## 7. React Frontend — Auth Pages

- [x] 7.1 Build Register page (form: email, display name, password)
- [x] 7.2 Build Login page (form: email, password)
- [x] 7.3 Add protected route wrapper that redirects unauthenticated users to login

## 8. React Frontend — Groups

- [x] 8.1 Build Home page listing all groups the current user belongs to
- [x] 8.2 Build Create Group modal/page
- [x] 8.3 Build Group Detail page showing member list and links to their wishlists
- [x] 8.4 Build Generate Invitation Link button (copies invite URL to clipboard)
- [x] 8.5 Build Join Group page that accepts an invitation code from the URL and calls the join endpoint

## 9. React Frontend — Own Wishlist

- [x] 9.1 Build My Wishlist page listing own items (no status shown)
- [x] 9.2 Build Add Item form (title, optional description, URL, price)
- [x] 9.3 Build Edit Item inline form or modal
- [x] 9.4 Build Delete Item button with confirmation

## 10. React Frontend — Viewing and Claiming Gifts

- [x] 10.1 Build Member Wishlist page showing another user's items with claim status badges (unclaimed / reserved / bought)
- [x] 10.2 Add "Reserve" button on unclaimed items (calls reserve endpoint)
- [x] 10.3 Add "Mark as Bought" button on items reserved by the current user
- [x] 10.4 Add "Release Claim" button on items claimed by the current user
- [x] 10.5 Disable Reserve button and show "Reserved by someone else" on items claimed by another user
