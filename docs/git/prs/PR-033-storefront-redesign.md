# PR-033 — Storefront redesign: header, catalog, product page and cart

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/storefront-catalog` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Apply the new design system (PR-031) and the price data (PR-032) to the customer storefront so it stops looking like a
default template, and make every page usable on a phone.

## Changes
- **Header:** brand mark, underlined active link, cart icon with a count badge, user avatar. On narrow screens the menu
  moves to its own horizontally scrollable row instead of overflowing.
- **Catalog:** breadcrumb and result count, a filter panel (search, category, brand, price range with the real bounds
  as a hint), removable chips for active filters with "Clear all", sorting (name, price, newest), square product cards
  with a soft placeholder when there is no photo, price ("From …" when variants differ), skeleton loading, an empty
  state and a compact numbered pager (`buildPages`, unit tested). The filter state lives in the URL, so results are
  shareable and the back button works. On a phone the panel collapses behind a "Filters" button and the grid is two
  columns.
- **Product page:** sticky gallery, option chips instead of a select, quantity stepper, details list, success message
  with a link to the cart.
- **Cart:** rows with a quantity stepper, inline remove, sticky order summary that stacks under the list on phones.
- Theme: card spacing is no longer applied inside multi-column layouts (`.cols`, `.layout`, `.checkout`, `.grids`).

## Verification
- Web build and Vitest 21/21 (4 new pager tests); admin build and Vitest 6/6 unchanged.
- Browsed against a real API with seeded data at desktop and phone width: catalog (URL-driven price filter and sort),
  mobile filters panel, product page, adding to the cart, cart, checkout.
- Not covered: the order pages and the admin console get the new tokens but have not been redesigned yet.
