# PR-036 — Admin console redesign

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/admin-redesign` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Give the administration console the same design language as the storefront and the mobile app, and make the first
screen useful: what needs attention is the first thing a person sees.

## Changes
- **Dashboard:** KPI cards for orders to process (paid, highlighted), processing, shipped and delivered, loaded with
  count-only requests (page size 1) and only for roles holding `orders.read_all`; section tiles with an arrow; the
  "Your access" card kept.
- **Orders:** status filter as chips instead of a select (paid first), status badges, total count, numbered pager,
  row action revealed on hover (always visible on touch), no wrapping of numbers and dates.
- **Order detail:** breadcrumb, status badge and a single primary action for the next fulfilment step (start
  processing → mark shipped → mark delivered) plus Cancel only while the state machine allows it, items with unit
  prices, shipping address and the history timeline.
- **Sign-in:** wordmark and a cleaner form with the right autocomplete hints.
- The shell (sidebar, top bar) already uses the new tokens from PR-031.
- Theme: the "no card margin inside a card grid" rule also covers the dashboard grids and the side column.

## Verification
- Admin build and Vitest 6/6; web build unaffected.
- Browsed against a real API with seeded data: admin sign-in, dashboard counters, orders list, order detail, and a real
  state change (Paid → Processing: the action became "Mark shipped" and the timeline gained the new step).
- Not covered: catalog, inventory, promotions, shipping and notifications admin pages only inherit the new tokens.
