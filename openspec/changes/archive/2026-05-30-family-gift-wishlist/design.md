## Context

Greenfield application for families and friend groups to coordinate gift-giving without spoiling surprises. The core invariant: a wishlist owner **must never see** who has claimed (or that anyone has claimed) their items. This constraint drives most architectural decisions.

Tech stack: TypeScript + React frontend, C# ASP.NET Core BFF, relational database (PostgreSQL).

## Goals / Non-Goals

**Goals:**
- User registration and authentication
- Group creation and invitation-based membership
- Personal wishlist management (add/edit/delete items)
- Gift claiming (reserved / bought) visible only to buyers in the same group

**Non-Goals:**
- Real-time notifications or email alerts (can be added later)
- Payment processing
- Mobile native apps
- Public wishlists (wishlists are only visible within shared groups)

## Decisions

### 1. BFF enforces the hidden-status invariant

The React frontend communicates exclusively through the C# BFF. The BFF owns the ownership check: when a user requests their own wishlist, the API response strips all claim data before returning. This ensures the invariant cannot be bypassed by a savvy frontend user intercepting API responses.

**Alternative considered**: Frontend-only filtering. Rejected — the owner could inspect raw network responses.

### 2. Data model

```
Users           (Id, Email, DisplayName, PasswordHash, CreatedAt)
Groups          (Id, Name, CreatedByUserId, CreatedAt)
GroupMembers    (GroupId, UserId, Role [Owner|Member], JoinedAt)
GroupInvitations (Id, GroupId, InviteCode, CreatedByUserId, ExpiresAt, UsedAt)
WishlistItems   (Id, OwnerId, Title, Description, Url, Price, CreatedAt, UpdatedAt)
GiftClaims      (Id, WishlistItemId, ClaimedByUserId, Status [Reserved|Bought], ClaimedAt)
```

`GiftClaims` has a unique constraint on `WishlistItemId` (one active claim per item at a time). Status transitions: `Reserved → Bought`, or claim can be released back to unclaimed.

**Alternative considered**: Storing status on `WishlistItems`. Rejected — it would associate the claim with the item record that the owner can access.

### 3. Visibility rules (enforced in BFF)

| Viewer | Whose list | Sees items | Sees claim status |
|--------|-----------|------------|-------------------|
| Owner  | Own list  | Yes        | No                |
| Buyer  | Others in same group | Yes | Yes (including their own claim indicator) |

A user can only view another user's wishlist if they share at least one group.

### 4. Authentication

ASP.NET Core Identity with JWT bearer tokens. Refresh tokens stored server-side. React stores the access token in memory (not localStorage) to reduce XSS risk.

### 5. Clean Architecture layers (C# BFF)

```
API (Controllers) → Application (Use Cases) → Domain (Entities, Rules) → Infrastructure (EF Core, DB)
```

Domain layer contains the visibility rules as pure logic. Application layer orchestrates them. Infrastructure has no business logic.

## Risks / Trade-offs

- **Concurrent claims** → Two buyers claim the same item simultaneously. Mitigated by the unique DB constraint on `WishlistItemId`; the second request receives a 409 Conflict.
- **Stale invitation links** → Expired invitations accumulate. Mitigated by `ExpiresAt` field and a background cleanup job (or lazy cleanup on use).
- **Group membership drift** → A user removed from a group should immediately lose access to that group's wishlists. Mitigated by checking group membership on every wishlist read, not caching membership.
- **JWT token leakage** → Storing token in memory means it's lost on page refresh, requiring re-login or silent refresh. Trade-off accepted for improved XSS resilience; a refresh token cookie (HttpOnly) handles silent renewal.
