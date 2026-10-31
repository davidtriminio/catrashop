# PR-032 — Catalog prices, price filter and price sorting

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/catalog-filters` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
The storefront redesign needs prices in the product list and a price filter. The list endpoint returned no price at all,
so a card could not show one without fetching every product detail.

## Changes
- `ProductListItem` gains `minPrice`, `maxPrice` and `currency` (lowest/highest **active** variant price).
- `GET /api/v1/catalog/products` accepts `minPrice` and `maxPrice` (a product matches when any active variant is in
  range) and `sort=price_asc|price_desc`. Negative values are ignored and swapped bounds are normalised, so a stale URL
  still returns products instead of an error.
- `GET /api/v1/catalog/price-range` returns the lowest and highest price across active products (used for the filter hint).
- Frontend models and `CatalogApi` expose the new fields (`ProductQuery.minPrice/maxPrice`, `priceRange()`).
- The read store keeps the module boundary: all of this is plain Catalog data, no cross-module call. A "in stock" filter
  was deliberately left out because availability lives in Inventory and needs its own read contract.

## Verification
- Solution builds with 0 warnings; every existing suite passes (architecture 66/66).
- Exercised against a real PostgreSQL with seeded data: totals, `min=15`, `max=15`, `15..25`, both sort directions,
  swapped bounds and the price-range endpoint all returned the expected products.
