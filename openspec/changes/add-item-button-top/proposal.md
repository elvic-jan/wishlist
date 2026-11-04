## Why

Issue #86: the "Add item" button only appears below the last item, so on a long wishlist you have to scroll to the bottom to add something.

## What Changes

On every wishlist the user can edit (My wishlist, and a wishlist they are a buddy for):

- The add button is always shown at the top of the list once the list has items. The empty state keeps its own button.
- The add button is also shown below the last item when the list has 4 or more items.
- The new-item form opens where the clicked button is (top or bottom).
- After adding, the form closes, the page stays where it is, and a short "Added" confirmation is shown (the new item lands at the end of the list, which stays oldest first).
- Release note (feature): "Moved the add item button to the top of the wishlist page. Lists with more than 3 items also show it at the bottom."

## Impact

- `src/Wishlist.Web/src/components/WishlistEditor.tsx` (used by My wishlist and a buddy's wishlist)
- Spec `wishlists`: new requirement "Add button placement"
- No API change.
