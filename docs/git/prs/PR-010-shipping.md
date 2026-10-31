# PR-010 — Shipping module

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/shipping` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 10: shipping methods, shipments with tracking, a shipment state machine, and fulfillment that drives the
order lifecycle (Section 7).

## Changes
- **Domain:** `ShippingMethod` (reference data), `Shipment` aggregate (one per order) owning `ShipmentEvent`
  timeline + `Destination` snapshot; `ShipmentStatus` + `ShipmentStateMachine`; unique tracking number.
- **Application:** `ShippingMethodService` (CRUD + list), `ShipmentService` (create/ship/deliver/cancel/get/track,
  customer-scoped lookup); DTOs, validators.
- **Infrastructure:** `ShippingDbContext` (shipping schema) with unique indexes (one shipment per order, unique
  tracking, unique method code), repositories, outbox publisher, design-time factory, **initial migration**
  (4 tables).
- **Orders contract (extended):** `IOrderFulfillment` (GetFulfillment + Processing/Shipped/Delivered),
  implemented by `OrderManagementService`.
- **API:** public method list + PII-limited tracking; customer "my order's shipment"; admin ops under
  `orders.manage`.
- **Tests:** `Catrashop.Modules.Shipping.Tests` — **10 unit tests**.
- ADR-015 (shipping & fulfillment integration).

## Technical decisions
- Shipping drives orders through `IOrderFulfillment`; **order transition applied before the shipment persists**,
  so order and shipment states can't drift (ADR-015).
- One shipment per order (unique index); unique tracking numbers; public tracking exposes no full address.
- **Known simplification:** shipping-method cost is not folded into the order total (totals fixed at checkout);
  wiring shipping selection into checkout is deferred and documented.

## Testing / verification
- `dotnet build` → **0/0**.
- **`dotnet test` → 10 passed / 0 failed** (shipment state machine incl. no-deliver-before-ship,
  no-cancel-after-ship; method code/currency normalization; transition-table theory).
- Migration validates: `shipping` schema with `shipping_methods`/`shipments`/`shipment_events`/`outbox_messages`;
  `ux_shipments_order`, `ux_shipments_tracking`, `ux_shipping_methods_code`.
- API boot: 8 modules `[...,"Shipping"]`; 12 shipping endpoints; admin shipment create → **401**. (Public
  `/methods` returns 500 only because Postgres isn't running — confirmed a `Database.Connection` error, not a bug.)
- ⚠️ **Deferred (no Postgres):** live create → ship → deliver driving a real order through its states.

## Review cycle
**🟢 APPROVE (simulated)** — Fulfillment drives the order through a clean contract with the transition applied
before the shipment is saved, so the two can't desync; tracking limits PII. The shipping-cost-in-total gap is
called out rather than hidden.

## Merge result
Merged into `develop` with `--no-ff`.
