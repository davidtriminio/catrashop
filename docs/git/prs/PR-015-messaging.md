# PR-015 — Messaging (Outbox → RabbitMQ → handlers)

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/messaging` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 15: publish the modules' transactional outboxes to RabbitMQ and react to events across modules — activating
the integration events deferred since each module was built (Section 15).

## Changes
- **Infrastructure/Messaging:** `IEventBus` + `RabbitMqEventBus` (lazy, self-healing topic-exchange publisher);
  `OutboxProcessor<TContext>` (per-module drain) + `OutboxPublisherService` (background); `IntegrationEventConsumerService`
  (durable queue, manual-ack, requeue-on-failure); `IntegrationEventDispatcher` + `IntegrationEventTypeRegistry`;
  `AddMessaging` / `AddOutboxProcessor<T>` DI helpers.
- **BuildingBlocks:** `IIntegrationEventHandler<TEvent>`.
- **Api/Integration (composition root):** handlers (kept out of modules so modules stay decoupled) —
  `CustomerRegistered` → provision customer + welcome; `OrderCreated/OrderPaid/OrderCancelled/PaymentFailed/
  ShipmentCreated/ShipmentDelivered` → notifications; `LowInventoryDetected`/`CouponRedeemed` → logging/analytics;
  `IntegrationSetup` wires processors, handlers, and the type registry; `Program` calls it; RabbitMq config +
  dev-only password (env-overridable) in appsettings.Development.json.
- **Tests:** `Catrashop.Infrastructure.Tests` — **6 unit tests** (registry + dispatcher).
- ADR-020.

## Technical decisions
- **Outbox → topic exchange**, routing key = event full name, MessageId = outbox row id; drain marks processed in
  the same context; failures retry (nothing lost, nothing published pre-commit).
- **At-least-once + idempotent handlers** (dispatch keyed on event id; provisioning get-or-create) — no inbox
  table needed.
- **Handlers at the composition root** → no module depends on another's events.
- **Resilient hosted services**: the app boots and stays healthy with RabbitMQ *and* Postgres down (logged
  retries, no crash) — matching the project's deferred-infra discipline.
- **Known limitations (ADR-020):** no DLQ/max-retry yet (poison messages requeue); `ProductVariantCreated →
  Inventory` and `OrderPaid → auto-shipment` not wired (missing event / needs a policy); live publish/consume
  deferred to Docker.

## Testing / verification
- `dotnet build` → **0/0**; `dotnet test` → **98 passed** (6 new messaging).
- **API boot with messaging + no infrastructure:** all 10 modules, `/health/live` = 200, stays up; publisher and
  consumer log retries with **no unhandled exception / no crash** (verified in the run log).
- ⚠️ **Deferred (no Docker):** live publish to RabbitMQ, consumer delivery, and end-to-end reactions.

## Review cycle
**🟢 APPROVE (simulated)** — The outbox drain and consumer are clean and resilient, handlers sit at the
composition root so modules stay decoupled, and at-least-once is handled by idempotent handlers rather than a new
store. The DLQ gap and the two un-wired reactions are called out, not hidden.

## Merge result
Merged into `develop` with `--no-ff`.
