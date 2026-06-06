## Why

Families and friend groups struggle to coordinate gift-giving without accidentally duplicating purchases or spoiling surprises. A shared wishlist platform where buyers can silently claim gifts solves the coordination problem while keeping the element of surprise intact for the recipient.

## What Changes

- Introduce a new web application where users can create accounts and join groups
- Each user maintains a personal wishlist of desired gifts (name, description, link, price)
- Members of a group can view each other's wishlists and claim gifts (reserved/bought)
- Gift status (reserved/bought) is hidden from the wishlist owner so surprises are preserved
- Group invitations allow families and friend circles to connect

## Capabilities

### New Capabilities

- `user-auth`: User registration, login, and profile management
- `groups`: Creating groups, inviting members, and managing group membership
- `wishlists`: Adding, editing, and removing items from a personal wishlist
- `gift-claiming`: Viewing others' wishlists and marking gifts as reserved or bought (hidden from owner)

### Modified Capabilities

## Impact

- New greenfield application — no existing code is affected
- Requires a backend API (C# BFF), React frontend, and a relational database for users, groups, wishlists, and gift claims
