# PR-005 — Inventory module

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/inventory` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 5: stock tracking per SKU with available/reserved accounting, movements, adjustments, low-stock thresholds,
and idempotent reservation/release/commit operations for the checkout flow — with overselling made impossible.

## Changes
- **Domain:** `InventoryItem` aggregate (on-hand/reserved, `available` computed) owning `StockMovement` (audit)
  and `Reservation` (Held/Released/Committed, expiry); invariants prevent overselling and below-reserved
  adjustments; low-stock notify-once-per-crossing.
- **Application:** `InventoryAdminService` (create/receive/adjust/threshold), `InventoryQueryService`,
  `InventoryOperationsService` implementing the public `IInventoryOperations`; abstractions, DTOs, validators.
- **Infrastructure:** `InventoryDbContext` (inventory schema), owned-collection configs with a unique
  `(item, reference)` index for idempotency, repository, projection read store, outbox publisher, design-time
  factory, **initial migration** (4 tables).
- **Public contract:** `IInventoryOperations` + integration events (`InventoryReserved`, `InventoryReleased`,
  `LowInventoryDetected`).
- **API:** admin endpoints under `inventory.manage`.
- **Tests:** new `CommerceHub.Modules.Inventory.Tests` — **12 domain unit tests**.
- ADR-011 (inventory model / reservations / idempotency).

## Technical decisions
- On-hand vs reserved buckets (not a single counter) so holds and sales are distinct and releasable (ADR-011).
- **Idempotency by reference** (order id) enforced in the aggregate *and* by a unique DB index; retries never
  double-hold/ship.
- **Atomic multi-line reserve** — all lines or none; no partial holds.
- Inventory references SKUs as strings only — no FK/coupling to Catalog (boundaries preserved). Auto-creation
  from `ProductVariantCreated` deferred to messaging (Phase 15).

## Testing / verification
- `dotnet build` → **0/0**.
- **`dotnet test` → 12 passed / 0 failed** (overselling prevention, reserve idempotency, release/commit lifecycle,
  expiry sweep, adjustment guard, low-stock notify-once). Runs without a database.
- Migration validates: `inventory` schema with `inventory_items`, `stock_movements`, `reservations`,
  `outbox_messages`; unique SKU + unique `(item, reference)` indexes.
- API boot: `modules: ["Identity","Catalog","Inventory"]`; 6 inventory endpoints; admin `POST /items` → **401**.
- ⚠️ **Deferred (no Postgres):** DB-level concurrency under contention (the unique index race).

## Review cycle
**🟢 APPROVE (simulated)** — Overselling is structurally prevented and covered by tests; idempotency is enforced
in both the aggregate and the schema; the module exposes only a contract + events to the rest of the system.

## Merge result
Merged into `develop` with `--no-ff`.
