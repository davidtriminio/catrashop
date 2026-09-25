# ADR-013: Order lifecycle & checkout orchestration

- **Status:** Accepted
- **Date:** 2026-05-27

## Context
Checkout spans three modules — Cart (read the cart), Inventory (hold stock), Orders (record the sale) — and the
order then moves through a lifecycle where illegal jumps (a Paid order returning to Pending, an unpaid order
shipping) must be impossible. There is no distributed transaction across the module schemas.

## Decision

### Order state machine
The `Order` aggregate is the only place status changes, and every change goes through a single
`Transition` guarded by an explicit `OrderStateMachine` transition table (Section 47). Semantic methods
(`MarkPaid`, `MarkShipped`, `Cancel`, …) wrap it; an illegal move returns a `Result` failure rather than throwing.
Each transition appends to an immutable status history ("historial").

### Checkout orchestration (no distributed transaction)
Steps run against each module's own unit of work, ordered so a failure is safe:
1. Read the active cart (`ICartQuery`); empty → fail.
2. **Idempotency:** if this cart already produced an order (`GetByCartIdAsync`, backed by a unique index on
   `cart_id`), return that order.
3. Compute totals — subtotal from captured line prices, `discount = 0` (promotions arrive Phase 11), simulated tax.
4. Build the order (still in memory) and **reserve inventory first** (`IInventoryOperations.ReserveAsync`, keyed
   on the **cart id**). If any line is short, nothing is persisted.
5. Persist the order (Orders UoW) + enqueue `OrderCreated` to the outbox; then mark the cart checked out.

Because the reservation reference is the cart id (not a fresh id), retries are idempotent; if the process dies
after reserving but before persisting the order, the reservation **expires** and the stock frees itself. Payment
(Phase 9) calls `IOrderWorkflow.ConfirmPayment` → `Order.MarkPaid` + `Inventory.Commit(cartId)`; cancellation
releases the reservation only while the order still holds one (`HoldsReservation`).

## Alternatives considered
- **One transaction across modules (shared connection/`TransactionScope`).** Rejected: couples the module
  schemas at the transaction level, undermining the boundaries the modular monolith exists to keep; the
  reserve-first + expiry + idempotency approach gives correctness without it.
- **A full saga/process manager.** Overkill for a linear, in-process flow at this stage; the explicit ordering +
  outbox is sufficient and far simpler. The seams (contracts + events) leave a saga open if fulfillment grows.

## Consequences
- Illegal state changes are impossible and centrally defined; the order carries its own audit trail.
- Checkout is idempotent (unique `cart_id`), never oversells (reserve-before-persist), and self-heals abandoned
  attempts via reservation expiry.
- Orders depends only on Cart's and Inventory's public `Contracts` — the first true multi-module orchestration,
  boundaries intact.

## Verification
- **13 domain unit tests pass** (totals; full happy-path lifecycle; rejected illegal transitions — no back-to-
  pending, no unpaid-ship; terminal states; payment retry). Run without a database.
- Cross-module orchestration (live reserve/commit/release + the unique-index idempotency) is verified once
  Postgres is running.
