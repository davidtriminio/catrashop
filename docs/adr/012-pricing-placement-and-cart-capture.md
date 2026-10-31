# ADR-012: Pricing placement and cart price capture

- **Status:** Accepted
- **Date:** 2026-05-17

## Context
The Cart needs a trustworthy unit price for each line. Prices must come from the server (a client must never be
able to set its own price), and a line's price should be **stable** once added even if the catalog price later
changes. The domain list (Section 7) names a Pricing bounded context, but the phased build order (Section 57) has
no dedicated Pricing phase, and Catalog was originally scoped to exclude price.

## Decision
1. **List price lives on the Catalog variant.** `ProductVariant` gains `Price` + `Currency`. This is the base/list
   price — the single source a customer sees on the product page.
2. **Catalog exposes a public price query** (`IProductCatalogQuery.GetPurchasableVariantAsync`) returning a
   variant's id, name, price, and currency **only if it belongs to an Active product and is an active variant**.
3. **The Cart captures the price at add-time.** On add-to-cart, the Cart calls the Catalog query and stores a
   snapshot (`UnitPrice`, `Currency`, `ProductName`) on the line. Re-adding the same variant merges quantity but
   keeps the originally-captured price. **Checkout re-validates** against the live price before creating an order.
4. **A future Pricing module layers on top.** Regional pricing, price lists, time-boxed prices and promotional
   pricing (Section 7, Phase 11) become a Pricing engine that *adjusts* the catalog list price behind the same
   `IProductCatalogQuery`-style seam — no Cart/Checkout change required.

This supersedes the earlier "price lives only in Pricing" framing: the **list** price is a catalog attribute; a
**pricing engine** is a separate, later concern.

## Alternatives considered
- **Client sends the price.** Rejected outright — trivially exploitable.
- **Cart reads price live at checkout only (not captured).** Rejected — the displayed cart total would drift as
  prices change mid-session; capturing gives a stable cart with an explicit re-validation step at checkout.
- **Build a full Pricing module now.** Rejected as premature (no phase, no rules yet); the seam keeps it open.

## Consequences
- Prices are always server-authoritative and cart totals are stable and reproducible.
- One extra in-process call per add-to-cart (cheap; same process).
- Cross-module: Cart references only Catalog's `Contracts` (the price query), not its internals — the boundary
  holds, and this is the first module-to-module in-process contract dependency in the codebase.

## Verification
- 8 Cart domain unit tests pass (captured-price stability, currency guard, merge, expiry, checked-out lock).
- Live price lookup + persistence deferred until Postgres is running.
