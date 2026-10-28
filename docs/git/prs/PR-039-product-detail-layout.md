# PR-039 — Product detail page layout

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/product-detail-layout` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
The product page used a huge square image and left a large empty area beside it. Rebuild it inside the existing
design system (`frontend/shared/theme.scss`) so it is compact and the space is filled with useful content.

## Changes
- **Gallery:** capped at 480 px wide, 4:3 stage with `object-fit: contain`, thumbnails when there is more than one
  image (the primary image is shown first); a letter placeholder when there is none.
- **Buy box:** a compact sticky card (brand, name, price, option chips, quantity stepper, add to cart / wishlist,
  two short delivery notes). On narrow screens it flows normally under the gallery.
- **Support sections:** "About this product", "Details" (category, brand, SKU of the selected option and the
  product attributes) and "Shipping & returns".
- **Related products:** up to four other products of the same category (category slug resolved from the category
  list, then a normal catalog search); navigating between them reloads the page (the component now follows the route
  parameter instead of reading a snapshot, which was a latent bug).
- Hard-coded colours on this page were replaced with theme tokens.

## Content to review
The "Shipping & returns" text is deliberately generic (costs and delivery times at checkout; returns follow the
store's policy). It states no windows, fees or promises: replace it with the store's real policy when known.

## Verification
- Web build and Vitest 30/30.
- Checked in the browser pane at 1280, 768 and 375 px (layout, thumbnails, sections, related products, clicking a
  related product loads it).
- **Not verified:** the page was served against a throwaway mock of the catalog endpoints, not the real API with
  seeded data (no PostgreSQL available to me); add-to-cart and wishlist were not exercised in the browser; no
  automated test covers this component.
