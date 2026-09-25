# ADR-020: Messaging — Outbox → RabbitMQ → in-process handlers

- **Status:** Accepted
- **Date:** 2026-07-22

## Context
Every module already writes integration events to its own transactional outbox (ADR-005), but nothing published
them — cross-module reactions were deferred to this phase. Phase 15 activates the flow: publish outbox rows to a
broker and have consumers react (notifications on order/payment/shipment events, customer provisioning on
registration, analytics), without coupling modules to each other.

## Decision
- **Transactional Outbox → RabbitMQ topic exchange.** A per-module `OutboxProcessor<TContext>` drains that
  module's unprocessed `outbox_messages` (oldest first, batched) and publishes each to a durable topic exchange
  (`commercehub.events`) with **routing key = the event's CLR full name** and **MessageId = the outbox row id**.
  The row is marked processed in the same context; a publish failure marks it (retry count + error) and stops the
  batch, so it is retried next cycle. Nothing is lost and nothing is published before its domain change committed.
- **One durable consumer queue** (`commercehub.consumers`) bound to exactly the registered routing keys. An async
  consumer reads with **manual ack**: a handled message is acked, a failing one is nacked and **requeued**.
- **Handlers live at the composition root** (`Api/Integration`), not inside modules — so **no module depends on
  another module's events**. A handler references the publishing module's event contract and the consuming
  module's public contract (`INotificationDispatcher`, `ICustomerProvisioning`, `IOrderWorkflow`,
  `IOrderFulfillment`). The dispatcher resolves all `IIntegrationEventHandler<TEvent>` for an event and invokes
  them in a fresh DI scope.
- **Idempotent consumers, no inbox table.** Delivery is at-least-once, so handlers are idempotent by design:
  notification dispatches key on the event id (the Notifications module dedupes on it), and provisioning is
  get-or-create. This avoids a second dedupe store while staying correct under redelivery.
- **A type registry** maps event full name → CLR type; it is populated at startup and is what the consumer binds
  its queue to and deserializes against. Unknown types are logged and ignored (never poison-loop). Types without a
  handler are still registered so publishing works and the design is future-proof.
- **Resilient background services.** The publisher and consumer never throw out of their loops: if RabbitMQ (or
  Postgres) is down they log and retry, and RabbitMQ automatic recovery reconnects dropped connections. **The app
  boots and stays healthy whether or not the infrastructure is running.**

## Reactions wired
- `CustomerRegistered` → provision the customer profile (get-or-create) **and** send a welcome notification.
  Registration now flows Identity → (event) → Customers, decoupling the two. (The endpoint's synchronous
  provisioning call is left as a harmless idempotent fallback so it also works with messaging off.)
- `OrderCreated` / `OrderPaid` / `OrderCancelled` / `PaymentFailed` / `ShipmentCreated` / `ShipmentDelivered`
  → customer notifications (the Phase 12 dispatcher, now event-driven as ADR-017 anticipated).
- `LowInventoryDetected` → ops logging; `CouponRedeemed` → analytics logging (demonstrates fan-out to observers).

## Alternatives considered
- **Handlers inside the consuming modules.** Rejected — it would make, e.g., Notifications depend on Orders'
  event contracts; the composition root keeps modules mutually decoupled.
- **A shared inbox/dedupe table.** Rejected for now — idempotent handlers cover at-least-once delivery without it.
- **In-memory/mediator dispatch instead of a broker.** Rejected — the master plan calls for RabbitMQ, and a real
  broker gives durability, retries, and a seam for n8n/external consumers (Phases 16–17).

## Consequences
- Cross-module workflows are event-driven and loosely coupled; adding a reaction is a handler + a registration.
- The same events are available to external systems (n8n) once they subscribe to the exchange.

## Known limitations (documented, not hidden)
- **No dead-letter queue / max-retry yet:** a persistently failing (poison) message requeues indefinitely.
  Adding a DLX + retry cap is Phase 20/hardening.
- **`ProductVariantCreated → Inventory` auto-tracking** and **`OrderPaid → auto-create shipment`** are *not*
  wired: Catalog doesn't emit a variant event yet, and auto-shipment needs a method-selection policy. Both are
  noted as future once those events/policies exist.
- **Live verification deferred to Docker:** publishing to a real broker, consumer delivery, and end-to-end
  reactions require RabbitMQ + Postgres running.

## Verification
- Solution builds **0/0**; **6 messaging unit tests** (type registry register/resolve/reject; dispatcher routes a
  known event to its handler, ignores unknown types, no-ops when no handler) — full suite **98 pass**.
- **API boot with messaging enabled and no infrastructure:** starts with all 10 modules, `/health/live` = 200,
  and stays up; the outbox publisher and consumer log retries (Postgres/RabbitMQ down) with **no unhandled
  exception and no crash** — proving the resilience contract.
- Deferred (no Docker): live publish/consume and the end-to-end reactions.
