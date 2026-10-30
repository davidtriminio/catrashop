# PR-008 — Orders module (checkout + state machine)

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/orders` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 8: the checkout workflow (Section 8) and the order lifecycle state machine (Section 47) — the first
orchestration spanning multiple modules.

## Changes
- **Domain:** `Order` aggregate owning `OrderItem`, `OrderStatusHistory`, and a `ShippingAddress` snapshot;
  `OrderStateMachine` transition table; totals (subtotal + simulated tax − discount); guarded transitions with
  audit history; `HoldsReservation`.
- **Application:** `OrderCheckoutService` (orchestrates cart→reserve→order→event), `OrderWorkflowService`
  (`IOrderWorkflow`: confirm/fail payment), `OrderManagementService` (cancel + admin fulfillment transitions),
  `OrderQueryService`; DTOs, validators, `OrderingOptions` (tax rate, hold minutes).
- **Infrastructure:** `OrdersDbContext` (orders schema), config with a **unique `cart_id` index** (checkout
  idempotency), repository, projection read store, outbox publisher, design-time factory, **initial migration**
  (4 tables).
- **Public contract:** `IOrderWorkflow` + events (`OrderCreated`, `OrderPaid`, `OrderCancelled`).
- **API:** customer endpoints (checkout, list/get mine, cancel) + admin endpoints (search, get any, process/ship/
  deliver/cancel) guarded by `orders.read_all` / `orders.manage`.
- **Tests:** `Catrashop.Modules.Orders.Tests` — **13 domain unit tests**.
- ADR-013 (order lifecycle & checkout orchestration).

## Technical decisions
- **No distributed transaction** — steps ordered (reserve before persist) + reservation keyed on cart id +
  expiry + unique `cart_id` = idempotent, oversell-proof, self-healing checkout (ADR-013).
- **All status changes funnel through one guarded transition** — illegal moves are impossible, centrally defined.
- Orders depends only on Cart's and Inventory's public `Contracts`.

## Testing / verification
- `dotnet build` → **0/0**.
- **`dotnet test` → 13 passed / 0 failed** (totals; full happy path; rejected illegal transitions incl.
  no-back-to-pending and no-unpaid-ship; terminal states; payment retry; transition table theory).
- Migration validates: `orders` schema with `orders`/`order_items`/`order_status_history`/`outbox_messages`;
  unique `ix_orders_number` and `ux_orders_cart`; owned shipping-address columns.
- API boot: 6 modules `[...,"Orders"]`; 10 order endpoints; checkout + admin → **401** unauthenticated.
- ⚠️ **Deferred (no Postgres):** live cross-module checkout (reserve/commit/release) and idempotency under load.

## Review cycle
**🟢 APPROVE (simulated)** — The reserve-before-persist ordering plus cart-keyed idempotency is the right call
over a cross-schema transaction; the state machine is centralized and well-covered by tests.

## Merge result
Merged into `develop` with `--no-ff`.
