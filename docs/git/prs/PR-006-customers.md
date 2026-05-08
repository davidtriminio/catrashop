# PR-006 — Customers module

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/customers` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 6: customer profiles, address book, preferences, and wishlist — each customer acting only on their own data.

## Changes
- **Domain:** `Customer` aggregate (id = user id) owning `Address` and `WishlistItem`; invariants — single default
  shipping/billing (first address auto-defaults), no duplicate wishlist entries.
- **Application:** `CustomerService` (provisioning + profile + preferences, implements `ICustomerProvisioning`),
  `AddressService`, `WishlistService`; DTOs; validators.
- **Infrastructure:** `CustomersDbContext` (customers schema), owned-collection configs (unique
  `(customer, product)` wishlist index), repository, design-time factory, **initial migration** (4 tables).
- **API:** `me`-scoped endpoints (profile, preferences, addresses incl. set-default, wishlist), all authenticated.
- **Tests:** `CommerceHub.Modules.Customers.Tests` — **8 domain unit tests**.

## Technical decisions
- **Customer id = user id (1:1 with Identity).** A caller can only ever load their own aggregate, so IDOR is
  impossible by construction — no per-request ownership check needed.
- **On-demand provisioning** via `ICustomerProvisioning` (idempotent get-or-create), called by the endpoints with
  the JWT email. When messaging lands (Phase 15), the `CustomerRegistered` consumer will call the same contract
  with the full name — no endpoint change required.
- References catalog products by id only (no cross-module FK).

## Testing / verification
- `dotnet build` → **0/0**.
- **`dotnet test` → 8 passed / 0 failed** (default-address rules, set-default unsets prior, unknown-address
  failures, wishlist dedup/remove, id = user id).
- Migration validates: `customers` schema with `customers`, `addresses`, `wishlist_items`, `outbox_messages`;
  unique wishlist index.
- API boot: `modules: ["Identity","Catalog","Inventory","Customers"]`; 8 customer endpoints; `GET /me` → **401**
  unauthenticated.
- ⚠️ **Deferred (no Postgres):** persistence round-trips.

## Review cycle
**🟢 APPROVE (simulated)** — Keying the aggregate on the user id removes an entire class of authorization bugs;
provisioning is idempotent and forward-compatible with the messaging phase.

## Merge result
Merged into `develop` with `--no-ff`.
