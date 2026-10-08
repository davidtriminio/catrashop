# ADR-029: In-process event transport for small hosts

- **Status:** Accepted
- **Date:** 2026-10-07

## Context
Production runs on a 1 OCPU / 1 GB Oracle VM that already hosts other APIs. RabbitMQ (Erlang VM) alone would
consume a large share of that memory, while the system only has one deployable unit (ADR-001) and one consumer
queue — the broker adds operations cost without adding isolation.

## Decision
- A new `IEventBus` implementation, `InProcessEventBus`, is selected with `Messaging:Transport=InProcess`
  (default stays `RabbitMq`). The existing `OutboxPublisherService` still drains each module's outbox; instead of
  publishing to a broker it dispatches straight to the registered handlers in a fresh DI scope.
- The `IntegrationEventConsumerService` and the RabbitMQ readiness check are not registered in that mode.
- Delivery semantics are unchanged: a handler failure throws, the outbox row stays unprocessed and is retried
  (handlers are already idempotent, ADR-020).

## Consequences
- ✅ No broker to run; saves several hundred MB on the micro VM.
- ✅ Same code path and contracts; switching back to RabbitMQ is configuration only.
- ⚠️ n8n automations that subscribe to the RabbitMQ exchange (ADR-021) do not receive events in this mode.
- ⚠️ Events are handled by the process that raised them; scaling out to several API instances would need the
  broker (or competing-consumer-safe handlers) again.
