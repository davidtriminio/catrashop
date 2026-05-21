# PR-007 — Cart module (+ Catalog variant pricing)

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/cart` → `develop`
- **Type:** `feat` / `fix`
- **Status:** ✅ Merged

## Objective
Phase 7: a persistent shopping cart with server-captured prices, currency safety, sliding expiry, and recovery —
plus the Catalog pricing needed to feed it, and a public contract for checkout.

## Changes
- **Catalog:** `ProductVariant` gains `Price` + `Currency` (migration `AddVariantPricing`); new public
  `IProductCatalogQuery.GetPurchasableVariantAsync` returning a purchasable variant's price/name.
- **Cart domain:** `Cart` aggregate (one active per customer) owning `CartItem`; captured price/name; single
  currency; merge-on-re-add; sliding 7-day expiry; `MarkCheckedOut`.
- **Cart application/infra:** `CartService` (get-or-create, add/update/remove/clear + public `ICartQuery`),
  repository, `CartDbContext` (cart schema) with a **partial unique index** (`status = 'Active'`) enforcing one
  active cart per customer, design-time factory, **initial migration** (3 tables).
- **API:** me-scoped cart endpoints (authenticated).
- **Tests:** `CommerceHub.Modules.Cart.Tests` — **8 domain unit tests**.
- **Fix:** committed the `Api.csproj` module `<ProjectReference>`s omitted in phases 4-6 (see below).
- ADR-012 (pricing placement & cart price capture).

## Technical decisions
- **Prices captured server-side** from Catalog at add-time; client can't set a price; checkout re-validates (ADR-012).
- **One active cart per customer** enforced by a Postgres partial unique index, not just app logic.
- **Sliding expiry + reset-on-recovery**: an abandoned cart is retrievable until it lapses, then reset to empty.
- Cart depends only on Catalog's `Contracts` (price query) — first module-to-module in-process contract dependency.

## Testing / verification
- `dotnet build` → **0/0**.
- **`dotnet test` → 8 passed / 0 failed** (currency guard, captured-price stability on re-add, set-zero-removes,
  clear resets currency, sliding expiry, checked-out lock).
- Migrations validate: Catalog `AddVariantPricing` (price/currency columns); Cart `carts`/`cart_items` with the
  partial unique `ux_carts_customer_active` (`filter: status = 'Active'`).
- API boot: `modules: [...,"Cart"]` (5 total); cart endpoints present; `GET /cart` → **401** unauthenticated.
- ⚠️ **Deferred (no Postgres):** live price lookup + persistence + the partial-index enforcement under contention.

## Review cycle
**🔴 REQUEST CHANGES (simulated)** — A clean clone of `develop` would not build: phases 4-6 committed the
`Program.cs` module registrations but not the matching `Api.csproj` `<ProjectReference>`s (they lived only in the
working tree). Fix and verify a clean build.
**🛠 Corrective commit** — `fix(api): commit module project references omitted in phases 4-6`; verified with a
build from the committed state (working changes stashed).
**🟢 APPROVE (simulated)** — Build integrity restored; price capture is server-authoritative; one-active-cart is
enforced at the database, not just in code.

## Merge result
Merged into `develop` with `--no-ff`.
