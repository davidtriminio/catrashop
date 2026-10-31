# PR-035 — Storefront orders and shared UI components

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/storefront-account` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Carry the redesign into the customer's order pages and give both Angular apps one source of truth for how a state is
shown. Order, payment and shipment statuses were each rendered differently (or not coloured at all in the admin).

## Changes
- **Shared UI** (`frontend/shared/ui`):
  - `cs-status-badge` — one status→tone mapping used everywhere: lime = paid and waiting, amber = processing,
    indigo = shipped / in transit, green = delivered, red = failed or cancelled, grey = the rest. Labels are
    humanised ("Payment pending"). Replaces the storefront-only `OrderStatusBadge`.
  - `cs-timeline` — vertical timeline, newest first, latest step highlighted; used for order history and shipments.
  - `shared/util/pagination.ts` — the pager helper moved out of the catalog so the orders pages and the admin use it.
- **Orders list:** rows instead of a wide table (order number and date, status badge, total, chevron), skeleton while
  loading, empty state, numbered pager; collapses into a two-line row on phones.
- **Order detail:** breadcrumb, status next to the number, item rows with a thumbnail, totals, shipping address,
  shipment and history as timelines, Pay / Cancel actions in the header.
- **Tracking page:** result card with the status badge and the shipment timeline.
- **Footer:** wordmark, tagline and links.
- Theme: no card spacing inside the KPI, tile, product and side-column grids.
- Account and sign-in/register pages were not redesigned: they inherit the tokens and were checked as they are.

## Verification
- Web build and Vitest 23/23 (2 new: tone mapping and badge rendering; pager tests moved with the helper).
- Browsed against a real API with seeded data: sign-in, orders list, order detail with history timeline.
