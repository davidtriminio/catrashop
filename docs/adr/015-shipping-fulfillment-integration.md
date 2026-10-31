# ADR-015: Shipping & fulfillment integration

- **Status:** Accepted
- **Date:** 2026-06-16

## Context
Fulfillment must produce shipments with tracking and move a paid order through Processing → Shipped → Delivered.
The order lifecycle lives in the Orders module (ADR-013), so Shipping must drive those transitions without
reaching into Orders' internals, and the two views (order status vs shipment status) must not drift apart.

## Decision
- **Shipping owns shipments; Orders owns order status.** Shipping drives the order via a new public contract,
  `IOrderFulfillment` (`GetFulfillment`, `StartProcessing`, `MarkShipped`, `MarkDelivered`), implemented by the
  Orders module. Shipping never touches order tables.
- **One shipment per order** (unique index), created by operations for a Paid/Processing order. Creating a
  shipment for a **Paid** order first calls `StartProcessing` (Paid → Processing), then records the shipment with
  a captured destination snapshot, the method's cost, and a generated **unique tracking number**.
- **Consistency ordering:** on ship/deliver, the order transition (its own unit of work) is applied **first**; the
  shipment is persisted only if that succeeds — so the shipment can never claim a state the order refused. Each
  order transition is state-machine-validated and effectively idempotent.
- **Shipping methods** are reference data with a flat cost and estimate. **Known simplification:** method cost is
  *not* folded into the order total in this phase (totals are fixed at checkout, before a method is chosen);
  wiring shipping selection + cost into checkout is deferred and noted. The shipment still records the cost.
- **Tracking** is public by tracking number but returns only a status timeline + destination city/country (no
  full address) to limit PII exposure.

## Alternatives considered
- **Let Orders' admin endpoints be the only way to ship/deliver.** Rejected as the primary path: fulfillment
  state (tracking, carrier) belongs in Shipping; those admin endpoints remain as manual overrides.
- **React to `OrderPaid` to auto-create shipments.** That's the eventual design once messaging lands (Phase 15);
  until then shipment creation is operator-triggered through the API.

## Consequences
- Order and shipment states stay in lock-step (order advances first); boundaries hold (contract-only dependency).
- Emits `ShipmentCreated` and `ShipmentDelivered` (the latter drives n8n workflow 4: notify + request a review).

## Verification
- **10 unit tests pass** (shipment state machine incl. no-deliver-before-ship and no-cancel-after-ship; method
  normalization). API boots with 8 modules; admin shipment creation → 401 unauthenticated.
- Deferred (no Postgres): the live create-ship-deliver flow driving a real order through its states.
