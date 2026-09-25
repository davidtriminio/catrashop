# PR-016 — n8n automations

> ⚠️ **Local workflow simulation.** No human reviewed this code.

- **Branch:** `feature/n8n-automations` → `develop`
- **Type:** `feat`
- **Status:** ✅ Merged

## Objective
Phase 16: event-driven n8n automations over the RabbitMQ exchange from Phase 15 (Section 16) — no backend change,
the exchange is the integration seam.

## Changes
- **`automations/n8n/`** — four importable workflow exports: welcome email (`CustomerRegistered`), order receipt
  (`OrderPaid`), review request (`ShipmentDelivered`, 3-day wait), low-stock ops alert (`LowInventoryDetected` →
  ops webhook). Each is *RabbitMQ trigger → Code (normalize) → action*.
- **`automations/rabbitmq/definitions.json`** — importable topology: the `commercehub.events` topic exchange
  (merges with the backend's runtime declaration) and four durable `n8n.*` queues bound by routing key
  (= event CLR full names).
- **`automations/README.md`** — setup (compose, import definitions, credentials, import + activate workflows) and
  limitations.
- ADR-021.

## Technical decisions
- **Subscribe to the exchange, don't call the backend** — automations are additive and decoupled; a topic
  exchange fans a copy to each n8n queue independently of the in-process consumer.
- **No secrets in source** — RabbitMQ/SMTP credentials and `OPS_WEBHOOK_URL` are referenced by name/env only;
  exported credential IDs are placeholders.
- **Automations, not core logic** — transactional notifications stay in the Notifications module (ADR-017); n8n
  adds outward, easily-iterated touches.
- **Known limitations (ADR-021):** receipt/review need an email lookup (events carry customerId, not email —
  configurable fallback for now); at-least-once dedupe via `EventId` not yet enforced; live import/run deferred to
  Docker.

## Testing / verification
- All five JSON files parse; the four workflow queues match the four declared queues/bindings; routing keys equal
  the backend event type full names registered in Phase 15. No secrets committed. Backend unchanged (still builds
  0/0, 98 tests) — this phase adds only `automations/` + docs.
- ⚠️ **Deferred (no Docker):** importing into n8n, wiring credentials, and observing end-to-end executions.

## Review cycle
**🟢 APPROVE (simulated)** — Clean event-driven integration: n8n rides the existing exchange with its own bound
queues, zero backend coupling, and no secrets in source. The address-enrichment gap and dedupe note are called out
rather than hidden.

## Merge result
Merged into `develop` with `--no-ff`.
