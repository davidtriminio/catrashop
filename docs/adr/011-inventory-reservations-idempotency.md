# ADR-011: Inventory model, reservations & idempotency

- **Status:** Accepted
- **Date:** 2026-04-22

## Context
Checkout must hold stock while an order is being placed and paid, without ever overselling, and must behave
correctly under retries (a client or the checkout saga may call reserve/release/commit more than once). See
Sections 9 (idempotency) and 48 (inventory consistency).

## Decision
- **State model per SKU:** track `on_hand` (physical) and `reserved` (held for pending orders); `available =
  on_hand - reserved`. Invariants enforced in the aggregate: `reserved ≤ on_hand` and `available ≥ 0` — so a
  reservation that would oversell simply fails.
- **Reservations** are first-class rows keyed by a **reference** (the order/checkout id) with a status
  (Held/Released/Committed) and an expiry. `commit` decrements both on-hand and reserved (units ship);
  `release` returns them to available.
- **Idempotency by reference:** re-reserving an existing reference is a no-op success; releasing/committing an
  unknown or already-processed reference is a no-op success. A unique `(item, reference)` index backs this at the
  database level. Retries therefore never double-hold or double-ship.
- **Atomic multi-line reserve:** the public `IInventoryOperations.ReserveAsync` reserves every line or none —
  the first failure aborts before the unit of work is saved, so there are no partial holds.
- **Expiry:** held reservations carry an expiry; a background sweep (Workers phase) releases expired holds so a
  failed/abandoned checkout doesn't strand stock.
- **Low-stock:** crossing the threshold downward emits `LowInventoryDetected` **once per crossing** (a flag resets
  when stock recovers) to avoid event spam; drives the n8n operations alert (Section 23).

## Alternatives considered
- **Decrement on-hand directly at checkout (no reserved bucket).** Simpler but conflates "held" with "sold" and
  makes releasing an abandoned cart error-prone. Rejected.
- **Optimistic-only concurrency with no reservation rows.** Loses the audit trail and idempotency key. Rejected.

## Consequences
- No overselling; retries are safe; abandoned checkouts self-heal via expiry.
- Cross-module: Inventory exposes only `IInventoryOperations` + integration events; it references SKUs as strings,
  never Catalog tables — boundaries stay clean. Auto-creating items from `ProductVariantCreated` arrives with
  messaging (Phase 15); until then items are created via the admin API.

## Verification
- **12 domain unit tests pass** covering overselling, idempotency, lifecycle, adjustment guards, and low-stock —
  run without a database. DB-level concurrency (the unique index under contention) is verified once Postgres is up.
